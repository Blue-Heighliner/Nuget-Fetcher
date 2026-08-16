namespace NugetFetcher.Tests;

public sealed class PackagePropsScannerTests : IDisposable
{
    private readonly string tempDirectory = Directory.CreateTempSubdirectory("NugetFetcherTests").FullName;
    private readonly PackagePropsScanner scanner = new();

    public void Dispose()
    {
        Directory.Delete(tempDirectory, recursive: true);
    }

    [Fact]
    public void Scan_ReadsExplicitPackageVersions()
    {
        string filePath = WriteFile("""
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageVersion Include="Serilog" Version="3.1.1" />
              </ItemGroup>
            </Project>
            """);

        IReadOnlyList<PackageRequest> result = scanner.Scan(filePath);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == "Newtonsoft.Json" && p.Version == "13.0.3" && !p.IsImplicit);
        Assert.Contains(result, p => p.Id == "Serilog" && p.Version == "3.1.1" && !p.IsImplicit);
    }

    [Fact]
    public void Scan_ResolvesCustomProperties()
    {
        string filePath = WriteFile("""
            <Project>
              <PropertyGroup>
                <MyProperty>5</MyProperty>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="Some.Package" Version="$(MyProperty)" />
              </ItemGroup>
            </Project>
            """);

        IReadOnlyList<PackageRequest> result = scanner.Scan(filePath);

        PackageRequest package = Assert.Single(result);
        Assert.Equal("Some.Package", package.Id);
        Assert.Equal("5", package.Version);
    }

    [Fact]
    public void Scan_ResolvesNestedProperties()
    {
        string filePath = WriteFile("""
            <Project>
              <PropertyGroup>
                <BaseVersion>2</BaseVersion>
                <MyProperty>$(BaseVersion).0.0</MyProperty>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="Some.Package" Version="$(MyProperty)" />
              </ItemGroup>
            </Project>
            """);

        IReadOnlyList<PackageRequest> result = scanner.Scan(filePath);

        PackageRequest package = Assert.Single(result);
        Assert.Equal("2.0.0", package.Version);
    }

    [Fact]
    public void Scan_ReadsImplicitPackagesFromComment()
    {
        string filePath = WriteFile("""
            <Project>
              <!--IMPLICIT
              Test.Package.Thing
              Another.Package
              Third.Test.Package
              -->
              <ItemGroup>
                <PackageVersion Include="Explicit.Package" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        IReadOnlyList<PackageRequest> result = scanner.Scan(filePath);

        Assert.Equal(4, result.Count);
        Assert.Contains(result, p => p.Id == "Test.Package.Thing" && p.Version == PackageRequest.RuntimeVersionPlaceholder && p.IsImplicit);
        Assert.Contains(result, p => p.Id == "Another.Package" && p.Version == PackageRequest.RuntimeVersionPlaceholder && p.IsImplicit);
        Assert.Contains(result, p => p.Id == "Third.Test.Package" && p.Version == PackageRequest.RuntimeVersionPlaceholder && p.IsImplicit);
        Assert.Contains(result, p => p.Id == "Explicit.Package" && p.Version == "1.0.0" && !p.IsImplicit);
    }

    private string WriteFile(string content)
    {
        string filePath = Path.Combine(tempDirectory, $"Directory.Packages.{Guid.NewGuid():N}.props");
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
