# ModernLauncher

A modern, open-source application launcher built with WPF and .NET 9.

## Features

- **Modern UI**: Clean, dark-themed interface
- **Application Discovery**: Automatically detects .exe files in the Apps folder
- **Search & Filter**: Quickly find applications by name
- **Favorites System**: Mark frequently used applications as favorites
- **File Watcher**: Automatically updates when apps are added/removed
- **Single File Distribution**: Self-contained executable

## Getting Started

1. Download the latest release (`ModernLauncher-v1.0.0.zip`)
2. Extract the ZIP file to your desired location
3. Place your executable files in the `Apps` folder
4. Run `ModernLauncher.exe`

## Building from Source

### Prerequisites
- .NET 9.0 SDK or later
- Windows OS

### Build Instructions
```bash
# Clone the repository
git clone <your-repo-url>
cd ModernLauncher

# Build the project
dotnet build --configuration Release

# Create distribution package
.\BuildPackages.bat
```

## Project Structure

- `MainWindow.xaml/cs` - Main application window and UI logic
- `Program.cs` - Application entry point
- `Apps/` - Directory for executable files to be launched
- `BuildPackages.bat` - Script to create distribution packages

## System Requirements

- Windows 10 or later
- .NET 9.0 Runtime (included in single-file distribution)

## License

This project is open source. Feel free to modify and distribute.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.
