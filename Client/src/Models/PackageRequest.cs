namespace BlueHeighliner.NugetFetcher.Models;

/// <summary>
/// A single package identity requested for download, together with the file it was scanned from.
/// </summary>
internal sealed record PackageRequest
{
    /// <summary>
    /// The version placeholder assigned to implicit packages. Resolved to the actual requested
    /// dotnet runtime version when the download is executed.
    /// </summary>
    public const string RuntimeVersionPlaceholder = "RUNTIME";

    /// <summary>
    /// The NuGet package id.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The requested package version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The path of the <c>Directory.Packages.props</c> file this request was scanned from.
    /// </summary>
    public required string SourceFile { get; init; }

    /// <summary>
    /// Whether this package was declared as an implicit package rather than an explicit <c>PackageVersion</c> entry.
    /// </summary>
    public required bool IsImplicit { get; init; }
}
