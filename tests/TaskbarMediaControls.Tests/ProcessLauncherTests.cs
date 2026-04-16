namespace TaskbarMediaControls.Tests;

public class ProcessLauncherTests {
    [Fact]
    public void Start_WhenPathIsEmpty_ShouldReturnInvalidPath() {
        var launcher = new ProcessLauncher();

        var result = launcher.Start(string.Empty, avoidDuplicateWhenRunningWithoutWindow: true);

        Assert.Equal(ProcessLaunchOutcome.InvalidPath, result.Outcome);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Start_WhenPathDoesNotExist_ShouldReturnInvalidPath() {
        var launcher = new ProcessLauncher();
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.exe");

        var result = launcher.Start(missingPath, avoidDuplicateWhenRunningWithoutWindow: true);

        Assert.Equal(ProcessLaunchOutcome.InvalidPath, result.Outcome);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Start_WhenFoobarModeAndShowCommandSucceeds_ShouldReturnSuccess() {
        var launcher = new ProcessLauncher();
        var whereExe = Path.Combine(Environment.SystemDirectory, "where.exe");
        if (!File.Exists(whereExe)) {
            return;
        }

        var result = launcher.Start(
            whereExe,
            avoidDuplicateWhenRunningWithoutWindow: true,
            playerType: FallbackPlayerType.Foobar
        );

        Assert.True(result.IsSuccess);
        Assert.Contains(
            result.Outcome,
            new[] { ProcessLaunchOutcome.LaunchedNewProcess, ProcessLaunchOutcome.RestoredExistingWindow }
        );
    }

    private sealed class TempFile : IDisposable {
        public TempFile(string extension) {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
            File.WriteAllBytes(Path, []);
        }

        public string Path { get; }

        public void Dispose() {
            try {
                File.Delete(Path);
            }
            catch {
                // Ignore cleanup failures.
            }
        }
    }
}
