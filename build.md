Run this to:
- close all running process of this
- build
- run new built exe

```bash
Get-Process TaskbarMediaControlsPlus -ErrorAction SilentlyContinue | Stop-Process -Force; dotnet build "TaskbarMediaControls.sln" -c Release; & "D:\_installed\VScode repos\_Extra_Work\TaskbarMediaControls\bin\Release\net8.0-windows10.0.19041.0\TaskbarMediaControlsPlus.exe"
```

Portable (framework-dependent) release ZIP (main release path):

```bash
$version = "1.0.0"; dotnet publish "TaskbarMediaControls.csproj" -c Release; $publishDir = "D:\_installed\VScode repos\_Extra_Work\TaskbarMediaControls\bin\Release\net8.0-windows10.0.19041.0\publish"; Copy-Item "D:\_installed\VScode repos\_Extra_Work\TaskbarMediaControls\scripts\shortcut-manager.bat" "$publishDir\shortcut-manager.bat" -Force; $zipPath = "D:\_installed\VScode repos\_Extra_Work\TaskbarMediaControls\bin\Release\TaskbarMediaControls-Plus-v$version-portable.zip"; if (Test-Path $zipPath) { Remove-Item $zipPath -Force }; Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath; Get-FileHash $zipPath -Algorithm SHA256
```

Optional installer:

```bash
dotnet publish "TaskbarMediaControls.csproj" -c Release
```
Then build installer via `setup.iss`.