# TaskbarMediaControls-plus

<img width="255" height="89" alt="showoff" src="https://github.com/user-attachments/assets/bf906225-287c-4b8a-9ffe-52dba4a35fb5" />

`TaskbarMediaControls-plus` is an independent fork of TaskbarMediaControls for Windows 11.
It keeps the lightweight tray-control workflow while improving fallback player launch behavior
and documenting the project for maintainable, release-friendly development.

## Upstream credit

This project is based on the original TaskbarMediaControls by `2latemc`.

- Original repository: [https://github.com/2latemc/TaskbarMediaControls](https://github.com/2latemc/TaskbarMediaControls)
- This fork: independent maintenance and release cadence under `TaskbarMediaControls-plus`

This repository keeps Apache 2.0 licensing and attribution requirements from upstream.

## What changed in plus (and why)

The most important plus changes are focused on fallback app launching, especially `foobar2000`
when it runs in tray/minimized states where the process window is difficult to restore.

- **Stronger foobar restore path:** tries `/show`, verifies restore attempts, then falls back to
  multiple `/command` variants if needed.
- **Window restore reliability:** adds top-level window enumeration for processes that do not expose
  a direct `MainWindowHandle` while still owning visible windows.
- **Safer launch decisions:** resolves foobar handling from executable path (not only exact configured fallback path),
  reducing duplicate launches for already-running instances.
- **Clearer error messaging:** explicitly reports when `/show` and `/command` fallback strategies both fail.
- **Expanded tests:** adds/updates tests around launch player resolution, duplicate-avoidance behavior,
  and foobar launch outcomes.

Detailed technical change notes are in [PLUS_CHANGES.md](./PLUS_CHANGES.md).

## Features

- Tray media controls: Previous, Play/Pause, Next
- Single-click and double-click action customization per icon
- Optional launch on Windows startup
- Fallback media application launch flow with player-type handling
- Hover metadata display (optional)

## Installation

1. Download the latest release from this repository's Releases page.
2. Run the executable (or installer package if provided).
3. Pin/move tray icons from "Show hidden icons" if needed.
4. Use tray right-click menu to open settings, configure startup, or exit.

## Build and run (local)

```powershell
dotnet restore "./TaskbarMediaControls.sln"
dotnet build "./TaskbarMediaControls.sln"
dotnet run --project "./TaskbarMediaControls.csproj"
```

More setup and testing guidance is in [DEVELOPMENT_GUIDE.md](./DEVELOPMENT_GUIDE.md).

## Compatibility and limitations

- Target platform: Windows 10/11 (`net8.0-windows10.0.19041.0`)
- Tray behavior can vary by media app session exposure
- VLC support is still limited

## Why a plus fork

`TaskbarMediaControls-plus` exists to:

- ship behavior updates faster for edge cases observed in real-world usage,
- keep fork-specific release notes and support expectations clear,
- remain fully transparent about upstream origin and derivative changes.
