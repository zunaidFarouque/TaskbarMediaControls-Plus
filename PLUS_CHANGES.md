# TaskbarMediaControls-plus Change Inventory

This document summarizes the meaningful code changes currently present in this fork compared to
the original baseline, with emphasis on user-visible behavior and intent.

## 1) Foobar restore behavior

### What changed
- `ProcessLauncher` now has a multi-stage restore flow for `FallbackPlayerType.Foobar`:
  - tries `/show`,
  - verifies restore in a retry loop,
  - falls back to `/command` variants:
    - `Foobar2000/Show main window`
    - `Show main window`
    - `Foobar2000/Toggle main window`
    - `Toggle main window`
- Candidate executable paths are gathered from running process module paths and requested path, then attempted.

### User impact
- Reduces "nothing happens" scenarios when foobar2000 is already running but minimized to tray.
- Improves chance that a running instance is surfaced instead of launching duplicates.

### Why
- Foobar window behavior is inconsistent across configurations and minimize-to-tray modes.
- A single launch argument was not reliable enough in all real-world states.

### Edge cases / limits
- Some environments may still block process introspection or foreground focus calls.
- Restore verification can still miss rare UI timing/race cases.

## 2) Window restore reliability for running processes

### What changed
- Existing-window restore now:
  - attempts normal `MainWindowHandle` restore path,
  - then enumerates top-level windows for the process when needed.
- Restore success is now validated with `IsWindowVisible` + non-iconic checks.

### User impact
- Better recovery from running-but-hidden player windows.
- Fewer false positives where app was "restored" but not actually visible.

### Why
- `MainWindowHandle` can be zero for tray/minimized states.
- A direct process window handle is not always available even when windows exist.

### Edge cases / limits
- Some windows are not safe/relevant to show, depending on the app.
- Windows API behavior can vary across app frameworks.

## 3) Launch decision logic in tray features

### What changed
- `TrayFeatureLogic` adds:
  - `ResolveLaunchPlayerType(...)`
  - `ShouldAvoidDuplicateWhenRunningWithoutWindow(...)`
  - `IsFoobarExecutablePath(...)`
- `TrayAppContext` now uses these helpers when opening media source/fallback paths.

### User impact
- Foobar-specific duplicate-avoidance can now apply even if media source path is not exactly
  the configured fallback path.

### Why
- Path matching by fallback path alone was too strict.
- Behavior should follow actual player type when discoverable from executable path.

### Edge cases / limits
- Executable-name heuristics currently key on `foobar2000.exe`.
- Custom wrappers/launchers may bypass this detection.

## 4) Error message UX

### What changed
- Foobar restore failure messages now explicitly mention both attempted strategies (`/show` and `/command` fallback).

### User impact
- Users get clearer diagnosis of what was attempted.

### Why
- Better transparency for troubleshooting and issue reports.

### Edge cases / limits
- Messaging describes strategy, not low-level Win32 failure reasons.

## 5) Test coverage updates

### What changed
- `TrayFeatureLogicTests` adds tests for:
  - foobar executable player-type resolution,
  - duplicate-avoidance behavior for foobar executable path.
- `ProcessLauncherTests` updates foobar scenario expectation to treat successful show-command launches as success.

### User impact
- Better regression protection for fork-specific launch behavior.

### Why
- New behavior needed direct automated validation.

### Edge cases / limits
- Launch-related tests still depend on host environment constraints and available binaries.
