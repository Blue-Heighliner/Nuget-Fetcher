namespace NugetFetcher.Services;

/// <summary>
/// Scans <c>Directory.Packages.props</c> files (NuGet Central Package Management) for package
/// versions and implicit package declarations.
/// </summary>
internal interface IPackagePropsScanner
{
    /// <summary>
    /// Scans a single <c>Directory.Packages.props</c> file. Implicit packages are assigned
    /// <see cref="PackageRequest.RuntimeVersionPlaceholder"/> as their version, to be resolved to the
    /// actual dotnet runtime version when the download is executed.
    /// </summary>
    /// <param name="filePath">The path of the file to scan.</param>
    /// <returns>The package requests found in the file.</returns>
    IReadOnlyList<PackageRequest> Scan(string filePath);
}

/// <inheritdoc cref="IPackagePropsScanner" />
internal sealed class PackagePropsScanner : IPackagePropsScanner
{
    private const int MaxPropertyResolutionPasses = 25;
    private const string ImplicitCommentMarker = "IMPLICIT";

    private static readonly Regex PropertyReferencePattern = new(
        @"\$\((?<name>[A-Za-z_][A-Za-z0-9_]*)\)", RegexOptions.Compiled);

    /// <inheritdoc />
    public IReadOnlyList<PackageRequest> Scan(string filePath)
    {
        XDocument document = XDocument.Load(filePath, LoadOptions.None);
        Dictionary<string, string> properties = ReadProperties(document);

        List<PackageRequest> requests = new();

        foreach (XElement packageVersion in document.Descendants().Where(e => e.Name.LocalName is "PackageVersion"))
        {
            string? id = (string?)packageVersion.Attribute("Include") ?? (string?)packageVersion.Attribute("Update");
            string? version = (string?)packageVersion.Attribute("Version");

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
                continue;

            requests.Add(new PackageRequest
            {
                Id = id.Trim(),
                Version = ResolveProperties(version, properties),
                SourceFile = filePath,
                IsImplicit = false,
            });
        }

        foreach (string packageId in ReadImplicitPackageIds(document))
        {
            requests.Add(new PackageRequest
            {
                Id = packageId,
                Version = PackageRequest.RuntimeVersionPlaceholder,
                SourceFile = filePath,
                IsImplicit = true,
            });
        }

        return requests;
    }

    private static Dictionary<string, string> ReadProperties(XDocument document)
    {
        Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase);

        foreach (XElement propertyGroup in document.Descendants().Where(e => e.Name.LocalName is "PropertyGroup"))
        {
            foreach (XElement property in propertyGroup.Elements())
            {
                properties[property.Name.LocalName] = property.Value.Trim();
            }
        }

        return properties;
    }

    private static string ResolveProperties(string value, IReadOnlyDictionary<string, string> properties)
    {
        string current = value;

        for (int pass = 0; pass < MaxPropertyResolutionPasses; pass++)
        {
            string next = PropertyReferencePattern.Replace(current, match =>
                properties.TryGetValue(match.Groups["name"].Value, out string? resolved) ? resolved : match.Value);

            if (next == current)
                break;

            current = next;
        }

        return current.Trim();
    }

    private static IEnumerable<string> ReadImplicitPackageIds(XDocument document)
    {
        foreach (XComment comment in document.DescendantNodes().OfType<XComment>())
        {
            string[] lines = comment.Value.Replace("\r\n", "\n").Split('\n');

            if (lines.Length == 0 || !lines[0].Trim().Equals(ImplicitCommentMarker, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (string line in lines.Skip(1))
            {
                string packageId = line.Trim();

                if (packageId.Length > 0)
                    yield return packageId;
            }
        }
    }
}
