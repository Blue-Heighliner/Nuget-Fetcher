# NugetFetcher

A desktop app (Avalonia/.NET) that scans one or more `Directory.Packages.props` files, resolves the
full NuGet dependency closure of the packages they declare against a target dotnet runtime version, and
downloads the resulting `.nupkg` files into a single output zip archive.

## Project Structure

- `Client/` — the Avalonia desktop application.
- `Tests/` — xUnit tests for the application.
- `NugetFetcher.slnx` — solution file referencing both projects.
- `Run.task` / `Publish.task` — AutoDev task scripts for running and publishing the app.

## Usage

1. Enter the target **Dotnet Runtime Version** (e.g. `10.0.5`) — this is the version implicit/runtime
   packages are resolved against.
2. Click **Browse** and select one or more `Directory.Packages.props` files. Each file's packages are
   scanned and merged into the list below, deduplicated by id and version and sorted alphabetically by
   package id. Repeat for as many files as needed; click **Clear** to empty the list and start over.
3. Click **Download...** and choose an output `.zip` path. The app resolves the full dependency closure
   of every listed package for the given runtime version, then downloads the resulting `.nupkg` files
   into that archive, showing the current package being processed and a progress indicator while it runs.
4. Any problems (a file that fails to scan, a package that fails to resolve or download) are reported in
   a popup once the operation finishes.

## Building & Running

```
dotnet build NugetFetcher.slnx
dotnet run --project Client/Client.csproj
```

## Testing

```
dotnet test NugetFetcher.slnx
```

## Publishing

`Publish.task` produces clean, self-contained, single-file executables for Windows and Linux under
`Build/`:

```
dotnet publish Client/Client.csproj -c Release -r linux-x64 -o Build/linux-x64
dotnet publish Client/Client.csproj -c Release -r win-x64 -o Build/win-x64
```
