namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class MainWindowViewModelTests
{
    private readonly Mock<IPackagePropsScanner> scanner = new();
    private readonly Mock<INugetPackageDownloader> downloader = new();

    [Fact]
    public void AddScannedPackages_DeduplicatesByIdAndVersion()
    {
        scanner.Setup(s => s.Scan(It.IsAny<string>())).Returns(new List<PackageRequest>
        {
            new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file1.props", IsImplicit = false },
            new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file2.props", IsImplicit = false },
            new() { Id = "Pkg.B", Version = "2.0.0", SourceFile = "file1.props", IsImplicit = false },
        });

        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);

        viewModel.AddScannedPackages("file1.props");

        Assert.Equal(2, viewModel.Packages.Count);
    }

    [Fact]
    public void AddScannedPackages_DoesNotRequireRuntimeVersion()
    {
        scanner.Setup(s => s.Scan(It.IsAny<string>())).Returns(new List<PackageRequest>
        {
            new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file1.props", IsImplicit = false },
        });

        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);

        viewModel.AddScannedPackages("file1.props");

        Assert.Single(viewModel.Packages);
    }

    [Fact]
    public void AddScannedPackages_SortsAlphabeticallyById()
    {
        scanner.Setup(s => s.Scan(It.IsAny<string>())).Returns(new List<PackageRequest>
        {
            new() { Id = "Zeta.Package", Version = "9.0.0", SourceFile = "file1.props", IsImplicit = false },
            new() { Id = "Alpha.Package", Version = "1.2.3", SourceFile = "file1.props", IsImplicit = false },
            new() { Id = "Mid.Package", Version = "2.0.0", SourceFile = "file1.props", IsImplicit = false },
        });

        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);

        viewModel.AddScannedPackages("file1.props");

        Assert.Equal(new[] { "Alpha.Package", "Mid.Package", "Zeta.Package" }, viewModel.Packages.Select(p => p.Id));
    }

    [Fact]
    public void RemovePackage_RemovesFromList()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);
        PackageRequest package = new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file.props", IsImplicit = false };
        viewModel.Packages.Add(package);

        viewModel.RemovePackage(package);

        Assert.Empty(viewModel.Packages);
    }

    [Fact]
    public void ClearPackages_RemovesEveryPackage()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);
        viewModel.Packages.Add(new PackageRequest { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file.props", IsImplicit = false });
        viewModel.Packages.Add(new PackageRequest { Id = "Pkg.B", Version = "2.0.0", SourceFile = "file.props", IsImplicit = false });

        viewModel.ClearPackages();

        Assert.Empty(viewModel.Packages);
    }

    [Theory]
    [InlineData("", false, false)]
    [InlineData("10.0.5", false, true)]
    [InlineData("", true, false)]
    [InlineData("10.0.5", true, false)]
    public void CanDownload_RequiresRuntimeVersionAndAtLeastOnePackage(string runtimeVersion, bool empty, bool expected)
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object) { DotnetRuntimeVersion = runtimeVersion };

        if (!empty)
        {
            viewModel.Packages.Add(new PackageRequest { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file.props", IsImplicit = false });
        }

        Assert.Equal(expected, viewModel.CanDownload);
    }

    [Fact]
    public async Task Download_DoesNothingWhenCanDownloadIsFalse()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);

        await viewModel.Download("output.zip", TestContext.Current.CancellationToken);

        downloader.Verify(
            d => d.Download(It.IsAny<IReadOnlyList<PackageRequest>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void AddScannedPackages_ReportsProblemWhenScanFails()
    {
        scanner.Setup(s => s.Scan(It.IsAny<string>())).Throws(new InvalidOperationException("boom"));
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);
        string? problem = null;
        viewModel.ProblemReported += (_, message) => problem = message;

        viewModel.AddScannedPackages("/some/Directory.Packages.props");

        Assert.Equal("Failed to scan Directory.Packages.props: boom", problem);
        Assert.Empty(viewModel.Packages);
    }

    [Fact]
    public void AddScannedPackages_AcrossFilesMergesAndKeepsSorted()
    {
        scanner.Setup(s => s.Scan("a")).Returns([new PackageRequest { Id = "B", Version = "1", SourceFile = "a", IsImplicit = false }]);
        scanner.Setup(s => s.Scan("b")).Returns([new PackageRequest { Id = "A", Version = "1", SourceFile = "b", IsImplicit = false }, new PackageRequest { Id = "b", Version = "1", SourceFile = "b", IsImplicit = false }]);
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);

        viewModel.AddScannedPackages("a");
        viewModel.AddScannedPackages("b");

        Assert.Equal(["A", "B"], viewModel.Packages.Select(p => p.Id));
    }

    [Fact]
    public async Task Download_PassesPackagesAndClearsBusyState()
    {
        MainWindowViewModel viewModel = CreateReady();
        downloader
            .Setup(d => d.Download(It.IsAny<IReadOnlyList<PackageRequest>>(), "10.0.5", "out.zip", It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await viewModel.Download("out.zip", TestContext.Current.CancellationToken);

        downloader.Verify(d => d.Download(It.Is<IReadOnlyList<PackageRequest>>(p => p.Count == 1), "10.0.5", "out.zip", It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(string.Empty, viewModel.CurrentTask);
        Assert.True(viewModel.CanDownload);
    }

    [Fact]
    public async Task Download_ReportsWarningsFromProgress()
    {
        MainWindowViewModel viewModel = CreateReady();
        downloader
            .Setup(d => d.Download(It.IsAny<IReadOnlyList<PackageRequest>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<PackageRequest>, string, string, IProgress<string>, CancellationToken>((_, _, _, progress, _) =>
            {
                progress.Report("Resolving Pkg.A 1.0.0...");
                progress.Report("Warning: could not find X.");
                progress.Report("Skipping Y: invalid version.");
            })
            .Returns(Task.CompletedTask);
        string? problem = null;
        viewModel.ProblemReported += (_, message) => problem = message;

        await viewModel.Download("out.zip", TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Contains("Warning: could not find X.", problem);
        Assert.Contains("Skipping Y", problem);
        Assert.DoesNotContain("Resolving", problem);
    }

    [Fact]
    public async Task Download_ReportsProblemWhenDownloaderThrows()
    {
        MainWindowViewModel viewModel = CreateReady();
        downloader
            .Setup(d => d.Download(It.IsAny<IReadOnlyList<PackageRequest>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        string? problem = null;
        viewModel.ProblemReported += (_, message) => problem = message;

        await viewModel.Download("out.zip", TestContext.Current.CancellationToken);

        Assert.Equal("Download failed: network down", problem);
        Assert.False(viewModel.IsBusy);
    }

    private MainWindowViewModel CreateReady()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object) { DotnetRuntimeVersion = "10.0.5" };
        viewModel.Packages.Add(new PackageRequest { Id = "Pkg.A", Version = "1.0.0", SourceFile = "f.props", IsImplicit = false });
        return viewModel;
    }

    [Fact]
    public void AddPackage_AddsEnteredPackageAndClearsInputs()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object) { NewPackageId = " Pkg.A ", NewPackageVersion = " 1.2.3 " };

        viewModel.AddPackage();

        PackageRequest package = Assert.Single(viewModel.Packages);
        Assert.Equal("Pkg.A", package.Id);
        Assert.Equal("1.2.3", package.Version);
        Assert.False(package.IsImplicit);
        Assert.Equal(string.Empty, viewModel.NewPackageId);
        Assert.Equal(string.Empty, viewModel.NewPackageVersion);
        Assert.False(viewModel.CanAddPackage);
    }

    [Fact]
    public void AddPackage_SkipsDuplicateAndKeepsSorted()
    {
        MainWindowViewModel viewModel = CreateReady();

        viewModel.NewPackageId = "pkg.a";
        viewModel.NewPackageVersion = "1.0.0";
        viewModel.AddPackage();
        viewModel.NewPackageId = "Aaa";
        viewModel.NewPackageVersion = "2.0.0";
        viewModel.AddPackage();

        Assert.Equal(["Aaa", "Pkg.A"], viewModel.Packages.Select(p => p.Id));
    }

    [Fact]
    public void AddPackage_ReportsProblemForInvalidVersion()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object) { NewPackageId = "Pkg.A", NewPackageVersion = "nope" };
        string? problem = null;
        viewModel.ProblemReported += (_, message) => problem = message;

        viewModel.AddPackage();

        Assert.Equal("'nope' is not a valid package version.", problem);
        Assert.Empty(viewModel.Packages);
    }

    [Theory]
    [InlineData("", "1.0.0", false)]
    [InlineData("Pkg.A", "", false)]
    [InlineData("Pkg.A", "1.0.0", true)]
    public void CanAddPackage_RequiresIdAndVersion(string id, string version, bool expected)
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object) { NewPackageId = id, NewPackageVersion = version };

        Assert.Equal(expected, viewModel.CanAddPackage);
    }

    [Fact]
    public void AddPackage_DoesNothingWhenInputsMissing()
    {
        MainWindowViewModel viewModel = new(scanner.Object, downloader.Object);

        viewModel.AddPackage();

        Assert.Empty(viewModel.Packages);
    }
}
