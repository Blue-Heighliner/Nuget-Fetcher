namespace BlueHeighliner.NugetFetcher.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddByConvention_RegistersInterfacesWithSameNamedImplementations()
    {
        ServiceCollection services = new();

        services.AddByConvention(typeof(NugetSource).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<NugetSource>(provider.GetRequiredService<INugetSource>());
        Assert.IsType<PackagePropsScanner>(provider.GetRequiredService<IPackagePropsScanner>());
        Assert.IsType<NugetPackageDownloader>(provider.GetRequiredService<INugetPackageDownloader>());
    }

    [Fact]
    public void AddByConvention_ReturnsSameCollection()
    {
        ServiceCollection services = new();

        Assert.Same(services, services.AddByConvention(typeof(NugetSource).Assembly));
    }

    [Fact]
    public void AddByConvention_SkipsInterfacesWithoutMatchingImplementation()
    {
        ServiceCollection services = new();

        services.AddByConvention(typeof(ServiceCollectionExtensionsTests).Assembly);

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IUnmatched));
    }

    private interface IUnmatched;
}
