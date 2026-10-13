# API

This is an application, not a library: it exposes no public API. Its internal seams are the scanner,
the downloader, and the main window view model that coordinates them. This document covers how those
pieces fit together at the level a contributor needs, without restating member-level detail already
in the source.

## Shape

`MainWindowViewModel` is the single coordinator. It depends on an `IPackagePropsScanner` (turns a
`Directory.Packages.props` file into `PackageRequest`s) and an `INugetPackageDownloader` (turns a list
of requests plus a runtime version into a zip of `.nupkg` files). The view binds to the view model and
holds no logic beyond file pickers and the problem popup.

## Flow

1. The user enters the target runtime version and picks one or more props files.
2. Packages come from scanning files, or from a manually entered id and version (validated as a NuGet
   version). Each file is scanned; the results are merged into the package list, deduplicated by id and version
   and kept sorted by id.
3. When a runtime version and at least one package are present, the download action becomes available.
4. Download resolves the dependency closure for the target framework derived from the runtime version,
   then writes each `.nupkg` into the output zip while reporting progress.
5. Warnings and failures collected along the way are shown together in one popup afterward.
