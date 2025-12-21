@echo off
echo Building Digital Measuring Stick...

dotnet publish -c Release -o publish

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build complete! The executable is in the 'publish' folder.
echo Run 'install.bat' to install the application.
echo.
pause
