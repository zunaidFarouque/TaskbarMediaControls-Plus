Run this to:
- close all running process of this
- build
- run new built exe

```bash
Get-Process TaskbarMediaControlsPlus -ErrorAction SilentlyContinue | Stop-Process -Force; dotnet build "TaskbarMediaControls.sln" -c Release; & "D:\_installed\VScode repos\_Extra_Work\TaskbarMediaControls\bin\Release\net8.0-windows10.0.19041.0\TaskbarMediaControlsPlus.exe"
```