namespace TaskbarMediaControls;

public enum TrayIconSlot {
    Previous,
    PlayPause,
    Next
}

public enum ClickAction {
    DoNothing,
    PlayPause,
    NextTrack,
    PreviousTrack,
    OpenSettings,
    OpenFallbackExecutable
}

public enum FallbackActionWhenMediaActive {
    OpenCurrentMediaAppOrFallback,
    OpenFallbackExecutable
}

public enum FallbackPlayerType {
    Other,
    Foobar
}

public enum FallbackOpenAction {
    None,
    OpenMediaSource,
    OpenFallback
}

public sealed class IconBehaviorSettings {
    public bool Visible { get; set; } = true;
    public ClickAction SingleClick { get; set; }
    public ClickAction DoubleClick { get; set; }
}

public sealed class AppSettings {
    public int ConfigVersion { get; set; }

    public IconBehaviorSettings PreviousIcon { get; set; } = new() {
        Visible = true,
        SingleClick = ClickAction.PreviousTrack,
        DoubleClick = ClickAction.DoNothing
    };

    public IconBehaviorSettings PlayPauseIcon { get; set; } = new() {
        Visible = true,
        SingleClick = ClickAction.PlayPause,
        DoubleClick = ClickAction.OpenFallbackExecutable
    };

    public IconBehaviorSettings NextIcon { get; set; } = new() {
        Visible = true,
        SingleClick = ClickAction.NextTrack,
        DoubleClick = ClickAction.DoNothing
    };

    public bool ShowHoverTrackInfo { get; set; } = true;
    public string FallbackExecutablePath { get; set; } = string.Empty;
    public FallbackActionWhenMediaActive FallbackActionWhenMediaActive { get; set; } =
        FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback;
    public FallbackPlayerType FallbackPlayerType { get; set; } = FallbackPlayerType.Other;
}
