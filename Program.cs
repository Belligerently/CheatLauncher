using System;
using System.Windows;

namespace ModernLauncher
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Normal application startup
            var mainApp = new App();
            var mainWindow = new MainWindow();
            mainApp.Run(mainWindow);
        }
    }
}
