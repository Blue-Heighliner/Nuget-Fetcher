namespace BlueHeighliner.NugetFetcher.Services;

/// <summary>
/// Resolves the full dependency closure of a set of requested packages and downloads their
/// <c>.nupkg</c> files into a single output zip archive.
/// </summary>
internal interface INugetPackageDownloader
{
    /// <summary>
    /// Resolves and downloads every requested package plus its transitive dependencies, and writes
    /// the resulting <c>.nupkg</c> files into a zip archive.
    /// </summary>
    /// <param name="packages">The explicitly requested packages.</param>
    /// <param name="dotnetRuntimeVersion">The dotnet runtime version packages should target.</param>
    /// <param name="outputZipPath">The path of the zip archive to create.</param>
    /// <param name="progress">Receives human-readable progress messages.</param>
    /// <param name="cancellation">A token used to cancel the operation.</param>
    Task Download(
        IReadOnlyList<PackageRequest> packages,
        string dotnetRuntimeVersion,
        string outputZipPath,
        IProgress<string> progress,
        CancellationToken cancellation);
}

/// <inheritdoc cref="INugetPackageDownloader" />
internal sealed class NugetPackageDownloader(IDotnetFrameworkMapper frameworkMapper, INugetSource source) : INugetPackageDownloader
{
    /// <inheritdoc />
    public async Task Download(
        IReadOnlyList<PackageRequest> packages,
        string dotnetRuntimeVersion,
        string outputZipPath,
        IProgress<string> progress,
        CancellationToken cancellation)
    {
        NuGetFramework targetFramework = frameworkMapper.ToTargetFramework(dotnetRuntimeVersion);
        ILogger logger = NullLogger.Instance;

        SourceRepository repository = Repository.Factory.GetCoreV3(source.Location);
        DependencyInfoResource dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>(cancellation)
            ?? throw new InvalidOperationException($"NuGet source '{source.Location}' does not support dependency resolution.");
        FindPackageByIdResource findPackageByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellation)
            ?? throw new InvalidOperationException($"NuGet source '{source.Location}' does not support package downloads.");

        using SourceCacheContext cache = new();

        Dictionary<string, SourcePackageDependencyInfo> resolved = new(StringComparer.OrdinalIgnoreCase);

        foreach (PackageRequest package in packages)
        {
            string requestedVersion = package.Version.Equals(PackageRequest.RuntimeVersionPlaceholder, StringComparison.OrdinalIgnoreCase)
                ? dotnetRuntimeVersion
                : package.Version;

            if (!NuGetVersion.TryParse(requestedVersion, out NuGetVersion? version))
            {
                progress.Report($"Skipping {package.Id}: invalid version '{requestedVersion}'.");
                continue;
            }

            await ResolveDependencies(
                new PackageIdentity(package.Id, version),
                targetFramework,
                dependencyInfoResource,
                findPackageByIdResource,
                cache,
                logger,
                resolved,
                progress,
                cancellation);
        }

        string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputZipPath));

        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await using (FileStream fileStream = new(outputZipPath, FileMode.Create, FileAccess.Write))
        using (ZipArchive archive = new(fileStream, ZipArchiveMode.Create))
        {
            foreach (SourcePackageDependencyInfo package in resolved.Values.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase))
            {
                cancellation.ThrowIfCancellationRequested();
                progress.Report($"Downloading {package.Id} {package.Version}...");

                await using FileStream staging = new(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);

                bool success = await findPackageByIdResource.CopyNupkgToStreamAsync(
                    package.Id, package.Version, staging, cache, logger, cancellation);

                if (!success)
                {
                    progress.Report($"Warning: failed to download {package.Id} {package.Version}.");
                    continue;
                }

                staging.Position = 0;
                ZipArchiveEntry entry = archive.CreateEntry($"{package.Id}.{package.Version.ToNormalizedString()}.nupkg");
                await using Stream entryStream = entry.Open();
                await staging.CopyToAsync(entryStream, cancellation);
            }
        }

        progress.Report($"Done. {resolved.Count} package(s) written to {outputZipPath}.");
    }

    private async Task ResolveDependencies(
        PackageIdentity identity,
        NuGetFramework targetFramework,
        DependencyInfoResource dependencyInfoResource,
        FindPackageByIdResource findPackageByIdResource,
        SourceCacheContext cache,
        ILogger logger,
        Dictionary<string, SourcePackageDependencyInfo> resolved,
        IProgress<string> progress,
        CancellationToken cancellation)
    {
        if (resolved.ContainsKey(identity.Id))
        {
            return;
        }

        progress.Report($"Resolving {identity.Id} {identity.Version}...");

        SourcePackageDependencyInfo? dependencyInfo = await dependencyInfoResource.ResolvePackage(
            identity, targetFramework, cache, logger, cancellation);

        if (dependencyInfo is null)
        {
            progress.Report($"Warning: could not find {identity.Id} {identity.Version} for {targetFramework.GetShortFolderName()}.");
            return;
        }

        resolved[identity.Id] = dependencyInfo;

        foreach (PackageDependency dependency in dependencyInfo.Dependencies)
        {
            if (resolved.ContainsKey(dependency.Id))
            {
                continue;
            }

            NuGetVersion? floor = dependency.VersionRange.MinVersion;

            if (floor is null)
            {
                continue;
            }

            IEnumerable<NuGetVersion> candidateStableVersions = floor.IsPrerelease
                ? await findPackageByIdResource.GetAllVersionsAsync(dependency.Id, cache, logger, cancellation)
                : [];

            NuGetVersion dependencyVersion = SelectPreferredVersion(dependency.VersionRange, candidateStableVersions);

            await ResolveDependencies(
                new PackageIdentity(dependency.Id, dependencyVersion),
                targetFramework,
                dependencyInfoResource,
                findPackageByIdResource,
                cache,
                logger,
                resolved,
                progress,
                cancellation);
        }
    }

    /// <summary>
    /// Picks the version to resolve for a dependency range: the lowest stable version satisfying the
    /// range, falling back to the range's own floor only when no stable version satisfies it (i.e. the
    /// dependency explicitly requires a pre-release version).
    /// </summary>
    /// <param name="range">The dependency's declared version range.</param>
    /// <param name="candidateVersions">
    /// Every published version of the dependency, used to find a stable match when the range's floor is
    /// itself a pre-release version. Ignored when the floor is already stable.
    /// </param>
    /// <returns>The version to resolve.</returns>
    internal NuGetVersion SelectPreferredVersion(VersionRange range, IEnumerable<NuGetVersion> candidateVersions)
    {
        NuGetVersion floor = range.MinVersion
            ?? throw new ArgumentException("Version range has no lower bound.", nameof(range));

        if (!floor.IsPrerelease)
        {
            return floor;
        }

        NuGetVersion? lowestStable = candidateVersions
            .Where(v => !v.IsPrerelease && range.Satisfies(v))
            .OrderBy(v => v)
            .FirstOrDefault();

        return lowestStable ?? floor;
    }
}
