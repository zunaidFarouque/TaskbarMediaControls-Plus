@echo off
setlocal EnableDelayedExpansion
title TaskbarMediaControls-plus Shortcut Manager

set "SCRIPT_DIR=%~dp0"
set "APP_EXE=%SCRIPT_DIR%TaskbarMediaControlsPlus.exe"
if not exist "%APP_EXE%" (
    set "APP_EXE=%SCRIPT_DIR%..\TaskbarMediaControlsPlus.exe"
)
if not exist "%APP_EXE%" (
    set "APP_EXE=%SCRIPT_DIR%..\bin\Release\net8.0-windows10.0.19041.0\TaskbarMediaControlsPlus.exe"
)
if not exist "%APP_EXE%" (
    echo Could not find TaskbarMediaControlsPlus.exe.
    echo Place this script in the same folder as the executable
    echo or inside a "scripts" subfolder under it.
    pause
    exit /b 1
)

for %%I in ("%APP_EXE%") do set "APP_DIR=%%~dpI"

set "DESKTOP_LINK=%USERPROFILE%\Desktop\TaskbarMediaControls-plus.lnk"
set "STARTMENU_LINK=%APPDATA%\Microsoft\Windows\Start Menu\Programs\TaskbarMediaControls-plus.lnk"
set "STARTUP_LINK=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\TaskbarMediaControls-plus.lnk"

set "TOGGLE_DESKTOP=0"
set "TOGGLE_STARTMENU=0"
set "TOGGLE_STARTUP=0"

:menu
cls
echo ==============================================
echo    TaskbarMediaControls-plus Shortcut Manager
echo ==============================================
echo App: %APP_EXE%
echo.
call :print_target 1 "Desktop" "%DESKTOP_LINK%" !TOGGLE_DESKTOP!
call :print_target 2 "Start Menu" "%STARTMENU_LINK%" !TOGGLE_STARTMENU!
call :print_target 3 "Startup" "%STARTUP_LINK%" !TOGGLE_STARTUP!
echo.
echo Type 1/2/3 to toggle selections.
echo Press Enter to apply selected toggles.
echo Type q to quit.
set /p "CHOICE=> "

if /i "%CHOICE%"=="q" exit /b 0
if "%CHOICE%"=="" goto apply
if "%CHOICE%"=="1" call :flip TOGGLE_DESKTOP & goto menu
if "%CHOICE%"=="2" call :flip TOGGLE_STARTMENU & goto menu
if "%CHOICE%"=="3" call :flip TOGGLE_STARTUP & goto menu
goto menu

:apply
if "!TOGGLE_DESKTOP!"=="1" call :toggle_shortcut "Desktop" "%DESKTOP_LINK%"
if "!TOGGLE_STARTMENU!"=="1" call :toggle_shortcut "Start Menu" "%STARTMENU_LINK%"
if "!TOGGLE_STARTUP!"=="1" call :toggle_shortcut "Startup" "%STARTUP_LINK%"

if "!TOGGLE_DESKTOP!!TOGGLE_STARTMENU!!TOGGLE_STARTUP!"=="000" (
    echo No toggles selected.
) else (
    echo.
    echo Done.
)
echo.
pause
exit /b 0

:print_target
set "IDX=%~1"
set "NAME=%~2"
set "LINK=%~3"
set "SELECTED=%~4"
set "CURRENT=OFF"
if exist "%LINK%" set "CURRENT=ON"
set "MARK= "
if "%SELECTED%"=="1" set "MARK=X"
echo [%IDX%] [%MARK%] %NAME% (current: %CURRENT%)
exit /b 0

:flip
set "FLAG=%~1"
if "!%FLAG%!"=="1" (
    set "%FLAG%=0"
) else (
    set "%FLAG%=1"
)
exit /b 0

:toggle_shortcut
set "TARGET_NAME=%~1"
set "LINK_PATH=%~2"

if exist "%LINK_PATH%" (
    del "%LINK_PATH%" >nul 2>&1
    if exist "%LINK_PATH%" (
        echo Failed to remove %TARGET_NAME% shortcut.
    ) else (
        echo Removed %TARGET_NAME% shortcut.
    )
    exit /b 0
)

for %%I in ("%LINK_PATH%") do (
    if not exist "%%~dpI" mkdir "%%~dpI" >nul 2>&1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$WshShell = New-Object -ComObject WScript.Shell; " ^
 "$Shortcut = $WshShell.CreateShortcut('%LINK_PATH%'); " ^
 "$Shortcut.TargetPath = '%APP_EXE%'; " ^
 "$Shortcut.WorkingDirectory = '%APP_DIR:~0,-1%'; " ^
 "$Shortcut.IconLocation = '%APP_EXE%,0'; " ^
 "$Shortcut.Save()"

if exist "%LINK_PATH%" (
    echo Added %TARGET_NAME% shortcut.
) else (
    echo Failed to add %TARGET_NAME% shortcut.
)
exit /b 0
