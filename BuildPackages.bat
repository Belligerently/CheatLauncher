@echo off
echo Building ModernLauncher (Open Source)...

:: Clean previous builds
if exist "dist" rmdir /s /q "dist"
mkdir "dist"

echo.
echo Building Single File Distribution...
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o "dist"

echo.
echo Setting up distribution...

:: Create Apps folder and copy any existing apps
mkdir "dist\Apps"

:: Copy existing apps if any exist in the source Apps folder
if exist "Apps\*.exe" (
    copy "Apps\*.exe" "dist\Apps\"
)

:: Create README file
echo ModernLauncher v1.0.0 - Open Source> "dist\README.txt"
echo.>> "dist\README.txt"
echo A modern application launcher for executable files.>> "dist\README.txt"
echo Run ModernLauncher.exe to start the application.>> "dist\README.txt"
echo.>> "dist\README.txt"
echo Features:>> "dist\README.txt"
echo - Launch .exe files from the Apps folder>> "dist\README.txt"
echo - Search and filter applications>> "dist\README.txt"
echo - Favorite applications for quick access>> "dist\README.txt"
echo - Modern UI with dark theme>> "dist\README.txt"
echo.>> "dist\README.txt"
echo This is open source software.>> "dist\README.txt"

echo.
echo Creating ZIP package...

:: Create distribution package
powershell -Command "Compress-Archive -Path 'dist\*' -DestinationPath 'ModernLauncher-v1.0.0.zip' -Force"

echo.
echo Package building complete!
echo Distribution: ModernLauncher-v1.0.0.zip
echo.
pause
