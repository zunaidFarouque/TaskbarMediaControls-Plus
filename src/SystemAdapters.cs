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
                if (foobarRestore == ExistingProcessState.RestoredExistingWindow) {
                    return new ProcessLaunchResult(ProcessLaunchOutcome.RestoredExistingWindow);
                }

                return new ProcessLaunchResult(
                    ProcessLaunchOutcome.FoobarRestoreFailed,
                    "Could not restore Foobar2000 from tray or minimized state."
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

            var hasMatchingProcessWithoutWindow = false;
            foreach (var process in Process.GetProcessesByName(processName)) {
                try {
                    var modulePath = process.MainModule?.FileName;
                    if (!string.Equals(modulePath, fullPath, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    var handle = process.MainWindowHandle;
                    if (handle == IntPtr.Zero) {
                        hasMatchingProcessWithoutWindow = true;
                        continue;
                    }

                    if (IsIconic(handle)) {
                        ShowWindow(handle, SwRestore);
                    }
                    else {
                        ShowWindow(handle, SwShow);
                    }

                    SetForegroundWindow(handle);
                    return ExistingProcessState.RestoredExistingWindow;
                }
                catch {
                    // Ignore protected or inaccessible process details.
                }
                finally {
                    process.Dispose();
                }
            }

            if (hasMatchingProcessWithoutWindow) {
                return ExistingProcessState.RunningWithoutWindow;
            }
        }
        catch {
            // If detection fails, caller will launch a new process.
        }

        return ExistingProcessState.None;
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
}

public sealed class StartupManager : IStartupManager {
    private const string RegistryRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryApprovedKey =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string AppName = "TaskbarMediaControls";

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
