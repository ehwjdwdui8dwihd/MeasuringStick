@echo off
echo Uninstalling Measuring Stick...

set "INSTALL_DIR=%LOCALAPPDATA%\MeasuringStick"

:: Kill the process if running
taskkill /F /IM MeasuringStick.exe 2>nul

:: Remove from startup registry
reg delete "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "MeasuringStick" /f 2>nul

:: Wait a moment for process to close
timeout /t 2 /nobreak >nul

:: Remove install directory
if exist "%INSTALL_DIR%" rmdir /S /Q "%INSTALL_DIR%"

echo.
echo Uninstallation complete!
echo.
pause
