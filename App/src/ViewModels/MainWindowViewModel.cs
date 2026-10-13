namespace BlueHeighliner.NugetFetcher.ViewModels;

/// <summary>
/// View model backing the main window: collects packages scanned from <c>Directory.Packages.props</c>
/// files and drives the download of their <c>.nupkg</c> closure into an output zip archive.
/// </summary>
internal sealed partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="scanner">Scans <c>Directory.Packages.props</c> files for package requests.</param>
    /// <param name="downloader">Downloads the resolved package closure into a zip archive.</param>
    public MainWindowViewModel(IPackagePropsScanner scanner, INugetPackageDownloader downloader)
    {
        this.scanner = scanner;
        this.downloader = downloader;
        DotnetRuntimeVersion = string.Empty;
        NewPackageId = string.Empty;
        NewPackageVersion = string.Empty;
        CurrentTask = string.Empty;
        Packages.CollectionChanged += (_, _) => UpdateCanDownload();
    }

    private readonly IPackagePropsScanner scanner;
    private readonly INugetPackageDownloader downloader;

    /// <summary>
    /// Raised when a problem occurs that should be reported to the user, e.g. via a popup.
    /// </summary>
    public event EventHandler<string>? ProblemReported;

    /// <summary>
    /// The dotnet runtime version packages should be resolved for, e.g. <c>10.0.5</c>.
    /// </summary>
    [ObservableProperty]
    public partial string DotnetRuntimeVersion { get; set; }

    /// <summary>
    /// The id of a package being entered manually, to be added with <see cref="AddPackage"/>.
    /// </summary>
    [ObservableProperty]
    public partial string NewPackageId { get; set; }

    /// <summary>
    /// The version of the package being entered manually, to be added with <see cref="AddPackage"/>.
    /// </summary>
    [ObservableProperty]
    public partial string NewPackageVersion { get; set; }

    /// <summary>
    /// Whether a manually entered package can currently be added: both id and version are entered and no
    /// download is in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool CanAddPackage { get; set; }

    /// <summary>
    /// Whether a download is currently in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Whether a download can currently be started: a runtime version has been entered, at least one
    /// package is queued, and no download is already in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool CanDownload { get; set; }

    /// <summary>
    /// The set of packages that will be downloaded, deduplicated by id and version and kept sorted
    /// alphabetically by <see cref="PackageRequest.Id"/>.
    /// </summary>
    public ObservableCollection<PackageRequest> Packages { get; } = new();

    /// <summary>
    /// A description of the task currently being performed by an in-progress download, e.g. which
    /// package is being downloaded.
    /// </summary>
    [ObservableProperty]
    public partial string CurrentTask { get; set; }

    /// <summary>
    /// Scans a <c>Directory.Packages.props</c> file and merges its packages into <see cref="Packages"/>,
    /// skipping any package id and version already present.
    /// </summary>
    /// <param name="filePath">The path of the file to scan.</param>
    public void AddScannedPackages(string filePath)
    {
        IReadOnlyList<PackageRequest> scanned;

        try
        {
            scanned = scanner.Scan(filePath);
        }
        catch (Exception ex)
        {
            ProblemReported?.Invoke(this, $"Failed to scan {Path.GetFileName(filePath)}: {ex.Message}");
            return;
        }

        foreach (PackageRequest package in scanned)
        {
            AddIfMissing(package);
        }

        SortPackages();
    }

    /// <summary>
    /// Adds the manually entered <see cref="NewPackageId"/> and <see cref="NewPackageVersion"/> to
    /// <see cref="Packages"/>, skipping it if already present. The download later gathers the package
    /// together with its full dependency closure, same as any scanned package. An unparseable version is
    /// reported as a problem instead.
    /// </summary>
    public void AddPackage()
    {
        if (!CanAddPackage)
        {
            return;
        }

        string id = NewPackageId.Trim();
        string version = NewPackageVersion.Trim();

        if (!NuGetVersion.TryParse(version, out _))
        {
            ProblemReported?.Invoke(this, $"'{version}' is not a valid package version.");
            return;
        }

        AddIfMissing(new PackageRequest { Id = id, Version = version, SourceFile = "Manual entry", IsImplicit = false });
        SortPackages();
        NewPackageId = string.Empty;
        NewPackageVersion = string.Empty;
    }

    private void AddIfMissing(PackageRequest package)
    {
        bool alreadyPresent = Packages.Any(existing =>
            string.Equals(existing.Id, package.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.Version, package.Version, StringComparison.OrdinalIgnoreCase));

        if (!alreadyPresent)
        {
            Packages.Add(package);
        }
    }

    /// <summary>
    /// Reorders <see cref="Packages"/> alphabetically by <see cref="PackageRequest.Id"/>.
    /// </summary>
    private void SortPackages()
    {
        List<PackageRequest> sorted = Packages
            .OrderBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(package => package.Version, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            if (!Equals(Packages[i], sorted[i]))
            {
                Packages[i] = sorted[i];
            }
        }
    }

    /// <summary>
    /// Removes a single package from <see cref="Packages"/>.
    /// </summary>
    /// <param name="package">The package to remove.</param>
    public void RemovePackage(PackageRequest package) => Packages.Remove(package);

    /// <summary>
    /// Removes every package from <see cref="Packages"/>.
    /// </summary>
    public void ClearPackages() => Packages.Clear();

    /// <summary>
    /// Resolves the dependency closure of every package in <see cref="Packages"/> and downloads it
    /// into the given output zip archive.
    /// </summary>
    /// <param name="outputZipPath">The path of the zip archive to create.</param>
    /// <param name="cancellation">A token used to cancel the download.</param>
    public async Task Download(string outputZipPath, CancellationToken cancellation)
    {
        if (!CanDownload)
        {
            return;
        }

        IsBusy = true;
        CurrentTask = string.Empty;
        List<string> warnings = new();

        try
        {
            DirectProgress progress = new(message =>
            {
                CurrentTask = message;

                if (message.Contains("Warning:", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("Skipping", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add(message);
                }
            });

            await downloader.Download(Packages.ToList(), DotnetRuntimeVersion, outputZipPath, progress, cancellation);

            if (warnings.Count > 0)
            {
                ProblemReported?.Invoke(this, string.Join(Environment.NewLine, warnings));
            }
        }
        catch (Exception ex)
        {
            ProblemReported?.Invoke(this, $"Download failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            CurrentTask = string.Empty;
        }
    }

    partial void OnDotnetRuntimeVersionChanged(string value) => UpdateCanDownload();

    partial void OnIsBusyChanged(bool value)
    {
        UpdateCanDownload();
        UpdateCanAddPackage();
    }

    partial void OnNewPackageIdChanged(string value) => UpdateCanAddPackage();

    partial void OnNewPackageVersionChanged(string value) => UpdateCanAddPackage();

    private void UpdateCanAddPackage() => CanAddPackage = !IsBusy && !string.IsNullOrWhiteSpace(NewPackageId) && !string.IsNullOrWhiteSpace(NewPackageVersion);

    private void UpdateCanDownload() => CanDownload = !IsBusy && Packages.Count > 0 && !string.IsNullOrWhiteSpace(DotnetRuntimeVersion);

    private sealed class DirectProgress(Action<string> report) : IProgress<string>
    {
        public void Report(string value) => report(value);
    }
}
