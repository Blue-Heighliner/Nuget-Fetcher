namespace NugetFetcher.Services;

/// <summary>
/// Maps a dotnet runtime version to the NuGet target framework packages should be resolved for.
/// </summary>
internal static class DotnetFrameworkMapper
{
    /// <summary>
    /// Resolves the NuGet target framework corresponding to a dotnet runtime version, e.g. <c>10.0.5</c> maps to <c>net10.0</c>.
    /// </summary>
    /// <param name="dotnetRuntimeVersion">The dotnet runtime version, e.g. <c>10.0.5</c>.</param>
    /// <returns>The resolved <see cref="NuGetFramework"/>.</returns>
    /// <exception cref="FormatException">The version could not be parsed.</exception>
    public static NuGetFramework ToTargetFramework(string dotnetRuntimeVersion)
    {
        NuGetVersion version = NuGetVersion.Parse(dotnetRuntimeVersion);

        string folder = version.Major >= 5
            ? $"net{version.Major}.{version.Minor}"
            : $"netcoreapp{version.Major}.{version.Minor}";

        return NuGetFramework.ParseFolder(folder);
    }
}
