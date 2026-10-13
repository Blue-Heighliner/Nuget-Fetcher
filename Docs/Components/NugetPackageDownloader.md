# NugetPackageDownloader

`NugetPackageDownloader` and `DotnetFrameworkMapper` resolve the dependency closure of the requested
packages against nuget.org and write the `.nupkg` files into one zip.

## Target framework

The runtime version maps to a framework folder: major 5 and above becomes `net<major>.<minor>`, older
versions become `netcoreapp<major>.<minor>`.

## Closure walk

Resolution is a depth-first walk keyed by package id, so each id is resolved once and the first version
reached wins. A package that cannot be found for the target framework is skipped with a warning rather
than failing the whole run. Zip entries are written in id order, and a failed individual download is
reported as a warning.

## Source and staging

The source comes from `INugetSource` (nuget.org by default), so a local feed folder can stand in for it
in integration tests. Each package is copied into a temporary file first and then into the zip, because
some sources (local feeds) require a seekable destination stream that a zip entry stream cannot provide.
A package that fails to download is reported as a warning and gets no zip entry.
