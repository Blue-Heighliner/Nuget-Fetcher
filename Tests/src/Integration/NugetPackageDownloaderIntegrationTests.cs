namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class NugetPackageDownloaderIntegrationTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("NugetFetcherFeed").FullName;
    private readonly string feed;
    private readonly string outputZip;
    private readonly List<string> messages = [];
    private readonly NugetPackageDownloader downloader;

    public NugetPackageDownloaderIntegrationTests()
    {
        feed = Path.Combine(root, "feed");
        outputZip = Path.Combine(root, "out", "packages.zip");
        Directory.CreateDirectory(feed);
        Mock<INugetSource> source = new();
        source.SetupGet(s => s.Location).Returns(feed);
        downloader = new NugetPackageDownloader(new DotnetFrameworkMapper(), source.Object);
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public async Task Download_WritesRequestedPackageAndTransitiveDependencies()
    {
        AddPackage("Root.Pkg", "1.0.0", ("Dep.One", "2.0.0"));
        AddPackage("Dep.One", "2.0.0", ("Dep.Two", "3.0.0"));
        AddPackage("Dep.Two", "3.0.0");

        await Run(Request("Root.Pkg", "1.0.0"));

        Assert.Equal(["Dep.One.2.0.0.nupkg", "Dep.Two.3.0.0.nupkg", "Root.Pkg.1.0.0.nupkg"], EntryNames());
        Assert.Contains(messages, m => m.StartsWith("Done. 3 package(s)"));
    }

    [Fact]
    public async Task Download_ResolvesImplicitPackagesAtRuntimeVersion()
    {
        AddPackage("Implicit.Ref", "10.0.5");

        await Run(Request("Implicit.Ref", PackageRequest.RuntimeVersionPlaceholder, isImplicit: true));

        Assert.Equal(["Implicit.Ref.10.0.5.nupkg"], EntryNames());
    }

    [Fact]
    public async Task Download_SharedDependencyIsWrittenOnce()
    {
        AddPackage("A", "1.0.0", ("Shared", "1.0.0"));
        AddPackage("B", "1.0.0", ("Shared", "1.0.0"));
        AddPackage("Shared", "1.0.0");

        await Run(Request("A", "1.0.0"), Request("B", "1.0.0"));

        Assert.Equal(["A.1.0.0.nupkg", "B.1.0.0.nupkg", "Shared.1.0.0.nupkg"], EntryNames());
    }

    [Fact]
    public async Task Download_PrefersLowestStableVersionOverPrereleaseFloor()
    {
        AddPackage("Root", "1.0.0", ("Dep", "1.0.0-preview.1"));
        AddPackage("Dep", "1.0.0-preview.1");
        AddPackage("Dep", "1.0.0");
        AddPackage("Dep", "2.0.0");

        await Run(Request("Root", "1.0.0"));

        Assert.Contains("Dep.1.0.0.nupkg", EntryNames());
    }

    [Fact]
    public async Task Download_SkipsInvalidVersionWithMessage()
    {
        await Run(Request("Bad.Pkg", "not-a-version"));

        Assert.Contains(messages, m => m.StartsWith("Skipping Bad.Pkg"));
        Assert.Empty(EntryNames());
    }

    [Fact]
    public async Task Download_WarnsWhenPackageIsMissing()
    {
        await Run(Request("Missing.Pkg", "1.0.0"));

        Assert.Contains(messages, m => m.StartsWith("Warning: could not find Missing.Pkg"));
        Assert.Empty(EntryNames());
    }

    [Fact]
    public async Task Download_SkipsDependencyWithoutLowerBound()
    {
        AddPackage("Root", "1.0.0", ("Dep", "(, 2.0.0]"));

        await Run(Request("Root", "1.0.0"));

        Assert.Equal(["Root.1.0.0.nupkg"], EntryNames());
    }

    [Fact]
    public async Task Download_ThrowsWhenCancelled()
    {
        AddPackage("Root", "1.0.0");
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            downloader.Download([Request("Root", "1.0.0")], "10.0.5", outputZip, new Progress<string>(), cancellation.Token));
    }

    private static PackageRequest Request(string id, string version, bool isImplicit = false)
        => new() { Id = id, Version = version, SourceFile = "Directory.Packages.props", IsImplicit = isImplicit };

    private async Task Run(params PackageRequest[] requests)
    {
        await downloader.Download(requests, "10.0.5", outputZip, new SynchronousProgress(messages), TestContext.Current.CancellationToken);
    }

    private List<string> EntryNames()
    {
        using ZipArchive archive = ZipFile.OpenRead(outputZip);
        return archive.Entries.Select(e => e.FullName).Order().ToList();
    }

    private void AddPackage(string id, string version, params (string Id, string Range)[] dependencies)
    {
        PackageBuilder builder = new()
        {
            Id = id,
            Version = NuGetVersion.Parse(version),
            Description = id,
        };
        builder.Authors.Add("test");
        builder.DependencyGroups.Add(new PackageDependencyGroup(
            NuGetFramework.Parse("net10.0"),
            dependencies.Select(d => new NuGet.Packaging.Core.PackageDependency(d.Id, VersionRange.Parse(d.Range)))));
        string marker = Path.Combine(root, "marker.txt");
        File.WriteAllText(marker, id);
        builder.Files.Add(new PhysicalPackageFile { SourcePath = marker, TargetPath = "content/marker.txt" });

        using FileStream stream = File.Create(Path.Combine(feed, $"{id}.{version}.nupkg"));
        builder.Save(stream);
    }

    private sealed class SynchronousProgress(List<string> sink) : IProgress<string>
    {
        public void Report(string value) => sink.Add(value);
    }
}
