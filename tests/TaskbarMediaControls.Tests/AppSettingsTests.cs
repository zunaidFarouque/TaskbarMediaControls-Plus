namespace TaskbarMediaControls.Tests;

public class AppSettingsTests {
    [Fact]
    public void Defaults_ShouldMatchExpectedActionsAndVisibility() {
        var settings = new AppSettings();

        Assert.True(settings.PreviousIcon.Visible);
        Assert.True(settings.PlayPauseIcon.Visible);
        Assert.True(settings.NextIcon.Visible);

        Assert.Equal(ClickAction.PreviousTrack, settings.PreviousIcon.SingleClick);
        Assert.Equal(ClickAction.PlayPause, settings.PlayPauseIcon.SingleClick);
        Assert.Equal(ClickAction.NextTrack, settings.NextIcon.SingleClick);
        Assert.Equal(ClickAction.DoNothing, settings.PreviousIcon.DoubleClick);
        Assert.Equal(ClickAction.OpenFallbackExecutable, settings.PlayPauseIcon.DoubleClick);
        Assert.Equal(ClickAction.DoNothing, settings.NextIcon.DoubleClick);
        Assert.Equal(
            FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback,
            settings.FallbackActionWhenMediaActive
        );
        Assert.Equal(FallbackPlayerType.Other, settings.FallbackPlayerType);
    }

    [Fact]
    public void Clone_ShouldCreateDeepCopy() {
        var original = new AppSettings {
            FallbackExecutablePath = @"C:\Apps\player.exe"
        };

        var clone = SettingsModelLogic.Clone(original);
        clone.PreviousIcon.Visible = false;
        clone.FallbackExecutablePath = @"C:\Other\new.exe";
        clone.FallbackActionWhenMediaActive = FallbackActionWhenMediaActive.OpenFallbackExecutable;
        clone.FallbackPlayerType = FallbackPlayerType.Foobar;

        Assert.True(original.PreviousIcon.Visible);
        Assert.Equal(@"C:\Apps\player.exe", original.FallbackExecutablePath);
        Assert.Equal(
            FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback,
            original.FallbackActionWhenMediaActive
        );
        Assert.Equal(FallbackPlayerType.Other, original.FallbackPlayerType);
    }

    [Theory]
    [InlineData(0, ClickAction.PreviousTrack, ClickAction.DoNothing)]
    [InlineData(1, ClickAction.PlayPause, ClickAction.OpenFallbackExecutable)]
    [InlineData(2, ClickAction.NextTrack, ClickAction.DoNothing)]
    [InlineData(77, ClickAction.NextTrack, ClickAction.DoNothing)]
    public void ClickMapping_ShouldResolvePerIndex(int index, ClickAction expectedSingle, ClickAction expectedDouble) {
        var settings = new AppSettings();

        Assert.Equal(expectedSingle, TrayFeatureLogic.GetSingleClickAction(settings, index));
        Assert.Equal(expectedDouble, TrayFeatureLogic.GetDoubleClickAction(settings, index));
    }

    [Fact]
    public void DefaultStoreConstructor_ShouldTargetExecutableDirectorySettingsFile() {
        var store = new AppSettingsStore();
        var field = typeof(AppSettingsStore).GetField("_settingsPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(field);
        var path = (string?)field!.GetValue(store);
        Assert.False(string.IsNullOrWhiteSpace(path));

        Assert.True(Path.IsPathRooted(path));
        Assert.Equal("settings.json", Path.GetFileName(path));
    }
}
