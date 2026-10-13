# PackagePropsScanner

`PackagePropsScanner` and `PackageRequest` turn a `Directory.Packages.props` file into the list of
packages to download.

## Property resolution

Versions may reference MSBuild properties (`$(Name)`) declared in any `PropertyGroup` in the same file.
Substitution repeats for a bounded number of passes so nested references resolve while a cyclic
definition cannot loop forever. Unknown properties are left as written.

## Implicit packages

A comment whose first line is `IMPLICIT` lists one package id per following line. These become requests
flagged implicit with a placeholder version, replaced by the runtime version at download time.
