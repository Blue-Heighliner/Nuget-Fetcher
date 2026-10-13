# Usage

Examples of using the app in different situations.

## Download a props file's packages

1. Enter the target **Dotnet Runtime Version**, e.g. `10.0.5`.
2. Click **Browse** and select a `Directory.Packages.props` file.
3. Click **Download...** and choose an output `.zip` path.

The zip contains every listed package plus its transitive dependencies as `.nupkg` files.

## Combine several props files

Click **Browse** repeatedly, or select several files at once. Packages are merged and deduplicated by
id and version; click **Clear** to start over.

## Fetch a single package with its dependencies

Instead of browsing to a props file, enter a package id and version (e.g. `Newtonsoft.Json` and
`13.0.3`) and click **Add**. The download includes that package plus every transitive dependency.

## Include implicit packages

Packages not declared as a `PackageVersion` can be listed in a comment at the top of the props file:

```xml
<!--IMPLICIT
Microsoft.NETCore.App.Ref
Microsoft.AspNetCore.App.Ref
-->
```

They are resolved at the runtime version entered in the app.
