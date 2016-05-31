using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CopyDb.Desktop.ViewModel;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace CopyDb.Desktop
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly MainViewModel _mainViewModel;
        private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

        public MainWindow()
        {
            InitializeComponent();
            _mainViewModel = (MainViewModel)DataContext;
            _mainViewModel.LogaMensagem += _mainViewModel_LogaMensagem;
        }

        private void _mainViewModel_LogaMensagem(string obj) => _synchronizationContext.Post(AppendText, obj);

        private void AppendText(object state)
        {
            txtLog.AppendText(state + Environment.NewLine);
            txtLog.ScrollToEnd();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
            => _mainViewModel.SenhaFirebird = GetPassword(sender);

        private void PasswordBox_PasswordChanged_1(object sender, RoutedEventArgs e)
            => _mainViewModel.SenhaMsSql = GetPassword(sender);

        private static string GetPassword(object sender) => ((PasswordBox)sender).Password;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Multiselect = false, AddExtension = true, Filter = "Bancos Firebird (*.gdb, *.fdb)|*.gdb;*.fdb", Title = "MIGRA DB" };
            openFileDialog.ShowDialog(this);
            _mainViewModel.ArquivoFirebird = openFileDialog.FileName;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_mainViewModel.MigracaoNaoIniciada)
                e.Cancel = true;
        }
    }
}