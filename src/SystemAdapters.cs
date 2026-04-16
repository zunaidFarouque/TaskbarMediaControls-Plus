using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TaskbarMediaControls;

public sealed class ClipboardService : IClipboardService {
    public void SetText(string text) {
        Clipboard.SetText(text);
    }
}

public sealed class ProcessLauncher : IProcessLauncher {
    private const int SwRestore = 9;
    private const int SwShow = 5;
    private const int FoobarRestoreVerifyDelayMs = 400;
    private const int FoobarRestoreVerifyAttempts = 10;
    private static readonly string[] FoobarShowMainWindowMenuCommandVariants = [
        "Foobar2000/Show main window",
        "Show main window",
        "Foobar2000/Toggle main window",
        "Toggle main window"
    ];

    public ProcessLaunchResult Start(
        string path,
        bool avoidDuplicateWhenRunningWithoutWindow = false,
        FallbackPlayerType playerType = FallbackPlayerType.Other
    ) {
        try {
            if (string.IsNullOrWhiteSpace(path)) {
                return new ProcessLaunchResult(ProcessLaunchOutcome.InvalidPath, "Executable path is empty.");
            }

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) {
                return new ProcessLaunchResult(ProcessLaunchOutcome.InvalidPath, "Executable path does not exist.");
            }

            if (playerType == FallbackPlayerType.Foobar) {
                var foobarRestore = TryRestoreExistingWindow(fullPath);

                // If foobar2000 is running but has no restorable window handle (e.g., "minimized to tray"),
                // re-run detection after issuing the show command.
                var isFoobar2000Executable = IsFoobar2000Executable(fullPath);
                var hasRunningProcessWithoutWindow = foobarRestore != ExistingProcessState.None ||
                                                      (isFoobar2000Executable && HasAnyMatchingProcessWithoutWindow(fullPath));

                var candidateExecutablePaths = GetCandidateExecutablePaths(fullPath);
                var startedAny = false;

                foreach (var candidatePath in candidateExecutablePaths) {
                    if (!TryStartProcess(candidatePath, "/show")) {
                        continue;
                    }

                    startedAny = true;
                    if (WaitForFoobarRestored(candidatePath)) {
                        return new ProcessLaunchResult(ProcessLaunchOutcome.RestoredExistingWindow);
                    }

                    // Manual testing shows foobar2000.exe /show works even when our window probing
                    // cannot immediately observe the tray-restored UI. Treat a successful /show
                    // invocation as success for foobar specifically.
                    return new ProcessLaunchResult(
                        hasRunningProcessWithoutWindow
                            ? ProcessLaunchOutcome.RestoredExistingWindow
                            : ProcessLaunchOutcome.LaunchedNewProcess
                    );
                }

                if (hasRunningProcessWithoutWindow) {
                    foreach (var candidatePath in candidateExecutablePaths) {
                        foreach (var commandVariant in FoobarShowMainWindowMenuCommandVariants) {
                            var commandArgs = $"/command:\"{commandVariant}\"";
                            if (!TryStartProcess(candidatePath, commandArgs)) {
                                continue;
                            }

                            startedAny = true;
                            if (WaitForFoobarRestored(candidatePath)) {
                                return new ProcessLaunchResult(ProcessLaunchOutcome.RestoredExistingWindow);
                            }
                        }
                    }

                    return new ProcessLaunchResult(
                        ProcessLaunchOutcome.FoobarRestoreFailed,
                        "Could not restore Foobar2000 from tray/minimized state using /show and the /command fallback."
                    );
                }

                return startedAny
                    ? new ProcessLaunchResult(ProcessLaunchOutcome.LaunchedNewProcess)
                    : new ProcessLaunchResult(
                        ProcessLaunchOutcome.FoobarRestoreFailed,
                        "Could not start Foobar2000 using /show."
                    );
            }

            var existing = TryRestoreExistingWindow(fullPath);
            if (existing == ExistingProcessState.RestoredExistingWindow) {
                return new ProcessLaunchResult(ProcessLaunchOutcome.RestoredExistingWindow);
            }

            if (existing == ExistingProcessState.RunningWithoutWindow && avoidDuplicateWhenRunningWithoutWindow) {
                return new ProcessLaunchResult(
                    ProcessLaunchOutcome.RunningWithoutWindow,
                    "Fallback application is running but does not expose a restorable window."
                );
            }

            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            return new ProcessLaunchResult(ProcessLaunchOutcome.LaunchedNewProcess);
        }
        catch (Exception ex) {
            return new ProcessLaunchResult(ProcessLaunchOutcome.Failed, ex.Message);
        }
    }

    private static ExistingProcessState TryRestoreExistingWindow(string fullPath) {
        try {
            var processName = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrWhiteSpace(processName)) {
                return ExistingProcessState.None;
            }

            var hasMatchingProcess = false;
            foreach (var process in Process.GetProcessesByName(processName)) {
                try {
                    var modulePath = process.MainModule?.FileName;
                    if (!string.Equals(modulePath, fullPath, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    hasMatchingProcess = true;

                    // Fast path: try the process-reported main window first.
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero && AttemptRestoreWindow(handle)) {
                        return ExistingProcessState.RestoredExistingWindow;
                    }

                    // Tray-minimized state can result in MainWindowHandle==0, but the process
                    // still owns one or more top-level windows that we can enumerate.
                    if (RestoreTopLevelWindowsForProcess((uint)process.Id)) {
                        return ExistingProcessState.RestoredExistingWindow;
                    }
                }
                catch {
                    // Ignore protected or inaccessible process details.
                }
                finally {
                    process.Dispose();
                }
            }

            if (hasMatchingProcess) {
                return ExistingProcessState.RunningWithoutWindow;
            }
        }
        catch {
            // If detection fails, caller will launch a new process.
        }

        return ExistingProcessState.None;
    }

    private static bool HasAnyMatchingProcessWithoutWindow(string fullPath) {
        try {
            var processName = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrWhiteSpace(processName)) {
                return false;
            }

            foreach (var process in Process.GetProcessesByName(processName)) {
                try {
                    if (process.MainWindowHandle == IntPtr.Zero) {
                        return true;
                    }
                }
                catch {
                    // Ignore protected or inaccessible process details.
                }
                finally {
                    process.Dispose();
                }
            }
        }
        catch {
            // If detection fails, caller will treat it as a normal launch attempt.
        }

        return false;
    }

    private static bool AttemptRestoreWindow(IntPtr handle) {
        if (IsIconic(handle)) {
            ShowWindow(handle, SwRestore);
        }
        else {
            ShowWindow(handle, SwShow);
        }

        SetForegroundWindow(handle);
        // For "minimise to tray/icon" states, we only want to count this as restored if
        // the window actually becomes visible (otherwise caller will fall back to /show).
        return !IsIconic(handle) && IsWindowVisible(handle);
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private static bool RestoreTopLevelWindowsForProcess(uint processId) {
        var anyRestored = false;

        EnumWindowsProc callback = (hWnd, _) => {
            uint windowProcessId;
            GetWindowThreadProcessId(hWnd, out windowProcessId);
            if (windowProcessId != processId) {
                return true;
            }

            if (IsIconic(hWnd)) {
                ShowWindow(hWnd, SwRestore);
            }
            else {
                ShowWindow(hWnd, SwShow);
            }

            SetForegroundWindow(hWnd);

            if (!IsIconic(hWnd) && IsWindowVisible(hWnd)) {
                anyRestored = true;
                return false; // stop enumeration once we restore at least one window
            }

            return true;
        };

        EnumWindows(callback, IntPtr.Zero);
        return anyRestored;
    }

    private static bool IsFoobar2000Executable(string fullPath) {
        try {
            var fileName = Path.GetFileName(fullPath);
            return string.Equals(fileName, "foobar2000.exe", StringComparison.OrdinalIgnoreCase);
        }
        catch {
            return false;
        }
    }

    private static string[] GetCandidateExecutablePaths(string requestedFullPath) {
        var processName = Path.GetFileNameWithoutExtension(requestedFullPath);
        var candidates = new System.Collections.Generic.List<string>();
        var seenPaths = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try {
            if (!string.IsNullOrWhiteSpace(processName)) {
                foreach (var process in Process.GetProcessesByName(processName)) {
                    try {
                        var modulePath = process.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(modulePath) && seenPaths.Add(modulePath)) {
                            candidates.Add(modulePath);
                        }
                    }
                    catch {
                        // Ignore protected or inaccessible process details.
                    }
                    finally {
                        process.Dispose();
                    }
                }
            }
        }
        catch {
            // If enumeration fails, we still fall back to the requestedFullPath candidate below.
        }

        if (seenPaths.Add(requestedFullPath)) {
            candidates.Add(requestedFullPath);
        }

        return candidates.ToArray();
    }

    private static bool TryStartProcess(string exePath, string arguments) {
        try {
            var started = Process.Start(new ProcessStartInfo(exePath, arguments) { UseShellExecute = true });
            started?.Dispose();
            return started != null;
        }
        catch {
            return false;
        }
    }

    private static bool WaitForFoobarRestored(string exePath) {
        for (var attempt = 0; attempt < FoobarRestoreVerifyAttempts; attempt++) {
            var restored = TryRestoreExistingWindow(exePath);
            if (restored == ExistingProcessState.RestoredExistingWindow) {
                return true;
            }

            System.Threading.Thread.Sleep(FoobarRestoreVerifyDelayMs);
        }

        return false;
    }

    private enum ExistingProcessState {
        None,
        RestoredExistingWindow,
        RunningWithoutWindow
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}

public sealed class StartupManager : IStartupManager {
    private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryApprovedKey =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string AppName = "TaskbarMediaControls-plus";

    public bool StartupEntryExists() {
        try {
            using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
            return runKey?.GetValue(AppName) != null;
        }
        catch {
            return false;
        }
    }

    public bool IsStartupEnabled() {
        try {
            using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
            if (runKey == null) {
                return false;
            }

            var value = runKey.GetValue(AppName)?.ToString();
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            value = value.Trim('"');
            string exePath = Path.GetFullPath(Application.ExecutablePath);
            return string.Equals(value, exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch {
            return false;
        }
    }

    public void SetStartup(bool enable) {
        string exePath = Path.GetFullPath(Application.ExecutablePath);
        string value = $"\"{exePath}\"";

        using var runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true)
                           ?? Registry.CurrentUser.CreateSubKey(RegistryRunKey);
        if (enable) {
            runKey.SetValue(AppName, value, RegistryValueKind.String);
        }
        else {
            runKey.DeleteValue(AppName, false);
        }

        using var approvedKey = Registry.CurrentUser.OpenSubKey(RegistryApprovedKey, true)
                                ?? Registry.CurrentUser.CreateSubKey(RegistryApprovedKey);
        if (enable) {
            byte[] enabledValue = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            approvedKey.SetValue(AppName, enabledValue, RegistryValueKind.Binary);
        }
        else {
            approvedKey.DeleteValue(AppName, false);
        }
    }
}
