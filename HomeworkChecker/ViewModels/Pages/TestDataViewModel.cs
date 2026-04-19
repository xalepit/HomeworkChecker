using System;
using System.Collections.Generic;
using System.Text;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    public partial class TestDataViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty]
        private string _testDataText = string.Empty;

        private bool _isInitialized = false;
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
