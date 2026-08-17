namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class MainWindowViewModelTests
{
    private readonly IPackagePropsScanner scanner = Substitute.For<IPackagePropsScanner>();
    private readonly INugetPackageDownloader downloader = Substitute.For<INugetPackageDownloader>();

    [Fact]
    public void AddScannedPackages_DeduplicatesByIdAndVersion()
    {
        scanner.Scan(Arg.Any<string>()).Returns(new List<PackageRequest>
        {
            new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file1.props", IsImplicit = false },
            new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file2.props", IsImplicit = false },
            new() { Id = "Pkg.B", Version = "2.0.0", SourceFile = "file1.props", IsImplicit = false },
        });

        MainWindowViewModel viewModel = new(scanner, downloader);

        viewModel.AddScannedPackages("file1.props");

        Assert.Equal(2, viewModel.Packages.Count);
    }

    [Fact]
    public void AddScannedPackages_DoesNotRequireRuntimeVersion()
    {
        scanner.Scan(Arg.Any<string>()).Returns(new List<PackageRequest>
        {
            new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file1.props", IsImplicit = false },
        });

        MainWindowViewModel viewModel = new(scanner, downloader);

        viewModel.AddScannedPackages("file1.props");

        Assert.Single(viewModel.Packages);
    }

    [Fact]
    public void AddScannedPackages_SortsAlphabeticallyById()
    {
        scanner.Scan(Arg.Any<string>()).Returns(new List<PackageRequest>
        {
            new() { Id = "Zeta.Package", Version = "9.0.0", SourceFile = "file1.props", IsImplicit = false },
            new() { Id = "Alpha.Package", Version = "1.2.3", SourceFile = "file1.props", IsImplicit = false },
            new() { Id = "Mid.Package", Version = "2.0.0", SourceFile = "file1.props", IsImplicit = false },
        });

        MainWindowViewModel viewModel = new(scanner, downloader);

        viewModel.AddScannedPackages("file1.props");

        Assert.Equal(new[] { "Alpha.Package", "Mid.Package", "Zeta.Package" }, viewModel.Packages.Select(p => p.Id));
    }

    [Fact]
    public void RemovePackage_RemovesFromList()
    {
        MainWindowViewModel viewModel = new(scanner, downloader);
        PackageRequest package = new() { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file.props", IsImplicit = false };
        viewModel.Packages.Add(package);

        viewModel.RemovePackage(package);

        Assert.Empty(viewModel.Packages);
    }

    [Fact]
    public void ClearPackages_RemovesEveryPackage()
    {
        MainWindowViewModel viewModel = new(scanner, downloader);
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
        MainWindowViewModel viewModel = new(scanner, downloader) { DotnetRuntimeVersion = runtimeVersion };

        if (!empty)
            viewModel.Packages.Add(new PackageRequest { Id = "Pkg.A", Version = "1.0.0", SourceFile = "file.props", IsImplicit = false });

        Assert.Equal(expected, viewModel.CanDownload);
    }

    [Fact]
    public async Task DownloadAsync_DoesNothingWhenCanDownloadIsFalse()
    {
        MainWindowViewModel viewModel = new(scanner, downloader);

        await viewModel.DownloadAsync("output.zip", CancellationToken.None);

        await downloader.DidNotReceive().DownloadAsync(
            Arg.Any<IReadOnlyList<PackageRequest>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }
}
