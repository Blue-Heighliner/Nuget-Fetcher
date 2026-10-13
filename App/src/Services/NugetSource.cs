namespace BlueHeighliner.NugetFetcher.Services;

/// <summary>
/// Identifies the NuGet source packages are resolved and downloaded from.
/// </summary>
internal interface INugetSource
{
    /// <summary>
    /// The source location: a v3 service index URL or a local feed folder path.
    /// </summary>
    string Location { get; }
}

/// <inheritdoc cref="INugetSource" />
internal sealed class NugetSource : INugetSource
{
    /// <inheritdoc />
    public string Location { get; } = "https://api.nuget.org/v3/index.json";
}
