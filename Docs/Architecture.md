# Architecture

This document explains the high-level design decisions behind the app's implementation —
*why* it's built the way it is, not the class-by-class mechanics of *how*.

## Closure resolution picks the lowest stable version

Dependencies are resolved to the lowest version their range allows, mirroring what NuGet restore itself
picks, rather than the newest available, so the downloaded set matches what a real restore would need.
When that floor is a pre-release, the lowest stable version satisfying the range is preferred, falling
back to the pre-release floor only if no stable version qualifies. The trade-off is an extra
all-versions query for pre-release floors only.

## Implicit packages are deferred to the runtime version

`Directory.Packages.props` files can list implicit (SDK-supplied) packages in a comment block. Their
versions are not knowable from the file, so they carry a placeholder that is substituted with the
runtime version entered at download time. This keeps scanning independent of the runtime version, so
files can be added before the version is typed.

## Services are registered by naming convention

Each `IThing` is bound to the same-named `Thing` class automatically, so adding a service needs no
registration code. The trade-off is that a mismatched name silently skips registration, surfacing only
when the dependency fails to resolve.

## Problems are reported after the operation, not during

Scan and download failures are surfaced through a single `ProblemReported` event and shown in a popup,
so one bad file or package never aborts the rest of the batch.
