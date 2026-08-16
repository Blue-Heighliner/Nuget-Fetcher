namespace NugetFetcher.Tests;

public sealed class DotnetFrameworkMapperTests
{
    [Theory]
    [InlineData("10.0.5", "net10.0")]
    [InlineData("8.0.0", "net8.0")]
    [InlineData("6.0.1", "net6.0")]
    [InlineData("3.1.0", "netcoreapp3.1")]
    public void ToTargetFramework_MapsRuntimeVersionToFramework(string runtimeVersion, string expectedFolder)
    {
        NuGetFramework framework = DotnetFrameworkMapper.ToTargetFramework(runtimeVersion);

        Assert.Equal(expectedFolder, framework.GetShortFolderName());
    }
}
