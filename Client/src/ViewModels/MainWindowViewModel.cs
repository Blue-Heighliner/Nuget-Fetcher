namespace NugetFetcher.ViewModels;

/// <summary>
/// View model backing the main window: collects packages scanned from <c>Directory.Packages.props</c>
/// files and drives the download of their <c>.nupkg</c> closure into an output zip archive.
/// </summary>
internal sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPackagePropsScanner scanner;
    private readonly INugetPackageDownloader downloader;

    /// <summary>
    /// The dotnet runtime version packages should be resolved for, e.g. <c>10.0.5</c>.
    /// </summary>
    [ObservableProperty]
    public partial string DotnetRuntimeVersion { get; set; }

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
    /// Raised when a problem occurs that should be reported to the user, e.g. via a popup.
    /// </summary>
    public event EventHandler<string>? ProblemReported;

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
        CurrentTask = string.Empty;
        Packages.CollectionChanged += (_, _) => UpdateCanDownload();
    }

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
            bool alreadyPresent = Packages.Any(existing =>
                string.Equals(existing.Id, package.Id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Version, package.Version, StringComparison.OrdinalIgnoreCase));

            if (!alreadyPresent)
                Packages.Add(package);
        }

        SortPackages();
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
                Packages[i] = sorted[i];
        }
    }

    /// <summary>
    /// Removes a single package from <see cref="Packages"/>.
    /// </summary>
    /// <param name="package">The package to remove.</param>
    public void RemovePackage(PackageRequest package)
    {
        Packages.Remove(package);
    }

    /// <summary>
    /// Removes every package from <see cref="Packages"/>.
    /// </summary>
    public void ClearPackages()
    {
        Packages.Clear();
    }

    /// <summary>
    /// Resolves the dependency closure of every package in <see cref="Packages"/> and downloads it
    /// into the given output zip archive.
    /// </summary>
    /// <param name="outputZipPath">The path of the zip archive to create.</param>
    /// <param name="cancellationToken">A token used to cancel the download.</param>
    public async Task DownloadAsync(string outputZipPath, CancellationToken cancellationToken)
    {
        if (!CanDownload)
            return;

        IsBusy = true;
        CurrentTask = string.Empty;
        List<string> warnings = new();

        try
        {
            Progress<string> progress = new(message =>
            {
                CurrentTask = message;

                if (message.Contains("Warning:", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("Skipping", StringComparison.OrdinalIgnoreCase))
                    warnings.Add(message);
            });

            await downloader.DownloadAsync(Packages.ToList(), DotnetRuntimeVersion, outputZipPath, progress, cancellationToken);

            if (warnings.Count > 0)
                ProblemReported?.Invoke(this, string.Join(Environment.NewLine, warnings));
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

    partial void OnIsBusyChanged(bool value) => UpdateCanDownload();

    private void UpdateCanDownload()
    {
        CanDownload = !IsBusy && Packages.Count > 0 && !string.IsNullOrWhiteSpace(DotnetRuntimeVersion);
    }
}
