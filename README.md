# NugetFetcher

[![Release](https://img.shields.io/github/v/release/Blue-Heighliner/Nuget-Fetcher)](https://github.com/Blue-Heighliner/Nuget-Fetcher/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Build](https://github.com/Blue-Heighliner/Nuget-Fetcher/actions/workflows/build.yml/badge.svg)](https://github.com/Blue-Heighliner/Nuget-Fetcher/actions/workflows/build.yml)
[![Coverage](.github/badges/badge_linecoverage.svg)](https://github.com/Blue-Heighliner/Nuget-Fetcher/actions/workflows/build.yml)

A desktop app built on [Avalonia](https://avaloniaui.net/) and .NET that scans one or more
`Directory.Packages.props` files, resolves the full NuGet dependency closure of the packages they
declare against a target dotnet runtime version, and downloads the resulting `.nupkg` files into a
single output zip archive. Third-party license notices are in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

## Requirements

- Windows 10/11, 64-bit, or Linux, x86-64 (glibc-based distro). No separate .NET runtime install
  needed — the executable is self-contained.

## Installing

Download the executable for your platform from the latest
[Release](https://github.com/Blue-Heighliner/Nuget-Fetcher/releases):

- **Windows** — download `BlueHeighliner.NugetFetcher.exe` and run it.
- **Linux** — download `BlueHeighliner.NugetFetcher`, mark it executable
  (`chmod +x BlueHeighliner.NugetFetcher`), and run it.

## Usage

1. Enter the target **Dotnet Runtime Version** (e.g. `10.0.5`).
2. Click **Browse** and select one or more `Directory.Packages.props` files.
3. Click **Download...** and choose an output `.zip` path.
4. Any problems are reported in a popup once the operation finishes.

## Documentation

| File | Covers |
| --- | --- |
| [Docs/Api.md](Docs/Api.md) | How the scanner, downloader and view model fit together |
| [Docs/Usage.md](Docs/Usage.md) | Usage examples |
| [Docs/Architecture.md](Docs/Architecture.md) | High-level design decisions |
| [Docs/Project.md](Docs/Project.md) | Scripts, publishing and CI |
| [Docs/Components/](Docs/Components) | Internal component deep dives |
