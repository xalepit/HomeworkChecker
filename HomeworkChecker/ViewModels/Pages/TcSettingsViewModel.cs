using HomeworkChecker.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    public partial class TcSettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private CompareOptions _compareSettings;

        public Task OnNavigatedToAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;
            //

            _isInitialized = true;
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;
    }
}
