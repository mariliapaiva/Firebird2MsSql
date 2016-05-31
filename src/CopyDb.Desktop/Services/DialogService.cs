using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace CopyDb.Desktop.Services
{
    public sealed class DialogService : IDialogService
    {
        public void ShowMessage(string message)
        {
            var metroWindow = GetMetroWindow();
            metroWindow.ShowMessageAsync("MIGRA DB", message);
        }

        private static MetroWindow GetMetroWindow()
        {
            var metroWindow = (MetroWindow)Application.Current.MainWindow;
            metroWindow.MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Accented;
            return metroWindow;
        }
    }
}