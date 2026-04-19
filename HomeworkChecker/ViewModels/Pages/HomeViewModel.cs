using System;
using System.Collections.Generic;
using System.Text;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _demoExePath = string.Empty;
        [ObservableProperty]
        private string _studentExePath = string.Empty;
        public Task OnNavigatedToAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;
            //

            _isInitialized = true;
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        [RelayCommand]
        private void OnSelectDemoExe()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择demo exe",
                CheckFileExists = true,
                Multiselect = false,
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                DemoExePath = dialog.FileName;
            }
        }

        [RelayCommand]
        private void OnSelectStudentExe()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择自己的exe",
                CheckFileExists = true,
                Multiselect = false,
                Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                StudentExePath = dialog.FileName;
            }
        }
    }
}
