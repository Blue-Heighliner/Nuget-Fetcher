namespace NugetFetcher.Tests;

public sealed class NugetPackageDownloaderTests
{
    [Fact]
    public void SelectPreferredVersion_ReturnsFloorWhenAlreadyStable()
    {
        VersionRange range = VersionRange.Parse("13.0.3");

        NuGetVersion selected = NugetPackageDownloader.SelectPreferredVersion(range, []);

        Assert.Equal(NuGetVersion.Parse("13.0.3"), selected);
    }

    [Fact]
    public void SelectPreferredVersion_PrefersLowestStableOverPrereleaseFloor()
    {
        VersionRange range = VersionRange.Parse("3.119.3-preview.1.1");
        List<NuGetVersion> candidates =
        [
            NuGetVersion.Parse("3.119.3-preview.1.1"),
            NuGetVersion.Parse("3.119.4-preview.1.1"),
            NuGetVersion.Parse("3.119.4"),
            NuGetVersion.Parse("3.120.0"),
        ];

        NuGetVersion selected = NugetPackageDownloader.SelectPreferredVersion(range, candidates);

        Assert.Equal(NuGetVersion.Parse("3.119.4"), selected);
    }

    [Fact]
    public void SelectPreferredVersion_FallsBackToPrereleaseFloorWhenNoStableSatisfiesRange()
    {
        VersionRange range = VersionRange.Parse("3.119.3-preview.1.1");
        List<NuGetVersion> candidates =
        [
            NuGetVersion.Parse("3.119.0"),
            NuGetVersion.Parse("3.119.3-preview.1.1"),
            NuGetVersion.Parse("3.119.3-preview.2"),
        ];

        NuGetVersion selected = NugetPackageDownloader.SelectPreferredVersion(range, candidates);

        Assert.Equal(NuGetVersion.Parse("3.119.3-preview.1.1"), selected);
    }
}
