# Cheat Launcher

A modern, open-source Cheat Launcher for Minecrafft built with WPF and .NET 9.

> [!NOTE]  
> The image shown when going to the site is not what the app looks like.

<p align="center">
  <img width="173" height="66" src="https://cdn.nest.rip/uploads/f0e8fdff-6775-42fd-bea3-fc630b57374d.png">
</p>

<p align="center">
  <img width="460" height="300" src="https://cdn.nest.rip/uploads/dc296ba4-3f69-4eee-a5a5-3b1732633f9a.png">
</p>

## Features

- **Modern UI**: Clean, dark-themed interface
- **Cheat Discovery**: Automatically detects .exe files in the Apps folder
- **Search**: Quickly find applications by name
- **Favorites System**: Mark frequently used applications as favorites
- **File Watcher**: Automatically updates when apps are added/removed
- **Single File Distribution**: Self-contained executable

## Getting Started

1. Download the latest release [`CheatLauncher-v1.0.0`](https://github.com/Belligerently/CheatLauncher/releases)
2. Extract the ZIP file to your desired location
3. Place your executable files in the `Apps` folder
4. Run `ModernLauncher.exe` (name of the exe will be changed)

## Building from Source

### Prerequisites
- .NET 9.0 SDK or later
- Windows OS

### Build Instructions
```bash
# Clone the repository
git clone https://github.com/Belligerently/CheatLauncher
cd CheatLauncher

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
