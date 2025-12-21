@echo off
echo Installing Measuring Stick...

set "INSTALL_DIR=%LOCALAPPDATA%\MeasuringStick"
set "EXE_NAME=MeasuringStick.exe"

:: Create install directory
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Copy the exe to install directory
copy /Y "%~dp0publish\%EXE_NAME%" "%INSTALL_DIR%\%EXE_NAME%"

if %ERRORLEVEL% neq 0 (
    echo Failed to copy executable. Make sure you ran 'publish.bat' first.
    pause
    exit /b 1
)

:: Add to startup registry
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "MeasuringStick" /t REG_SZ /d "\"%INSTALL_DIR%\%EXE_NAME%\"" /f

echo.
echo Installation complete!
echo The app has been installed to: %INSTALL_DIR%
echo It will start automatically when Windows starts.
echo.
echo Starting the application now...
start "" "%INSTALL_DIR%\%EXE_NAME%"

pause
