namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class NugetSourceTests
{
    [Fact]
    public void Location_IsNugetOrgServiceIndex() => Assert.Equal("https://api.nuget.org/v3/index.json", new NugetSource().Location);
}
