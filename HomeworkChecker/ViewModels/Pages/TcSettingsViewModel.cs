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
        private string _trimMode; //占位
        [ObservableProperty]
        private int _lineSkipText; //占位
        [ObservableProperty]
        private int _lineOffsetText; //占位
        [ObservableProperty]
        private bool _ignoreBlank; //占位
        [ObservableProperty]
        private bool _crCrLfNotEqual; //占位
        [ObservableProperty]
        private int maxDiffText; //占位
        [ObservableProperty]
        private int maxLineText; //占位
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
