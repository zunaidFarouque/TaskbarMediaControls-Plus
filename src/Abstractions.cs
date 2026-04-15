namespace TaskbarMediaControls;

public interface IAppSettingsStore {
    AppSettings Load();
    void Save(AppSettings settings);
}

public interface IMediaSessionService : IDisposable {
    event Action<MediaSessionInfo>? MediaInfoChanged;
    Task InitializeAsync();
    Task<MediaSessionInfo> GetCurrentInfoAsync();
    Task RefreshAsync();
}

public interface IMediaMetadataProvider : IDisposable {
    event Action<MediaSessionInfo>? MediaInfoChanged;
    Task InitializeAsync();
    Task<MediaSessionInfo> GetCurrentInfoAsync();
}

public interface IClipboardService {
    void SetText(string text);
}

public interface IProcessLauncher {
    ProcessLaunchResult Start(
        string path,
        bool avoidDuplicateWhenRunningWithoutWindow = false,
        FallbackPlayerType playerType = FallbackPlayerType.Other
    );
}

public interface IStartupManager {
    bool StartupEntryExists();
    bool IsStartupEnabled();
    void SetStartup(bool enable);
}

public enum ProcessLaunchOutcome {
    RestoredExistingWindow,
    LaunchedNewProcess,
    RunningWithoutWindow,
    FoobarRestoreFailed,
    InvalidPath,
    Failed
}

public readonly record struct ProcessLaunchResult(ProcessLaunchOutcome Outcome, string? ErrorMessage = null) {
    public bool IsSuccess =>
        Outcome == ProcessLaunchOutcome.RestoredExistingWindow ||
        Outcome == ProcessLaunchOutcome.LaunchedNewProcess;
}
