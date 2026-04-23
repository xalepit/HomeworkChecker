using HomeworkChecker.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    public partial class TestDataViewModel : ObservableObject, INavigationAware
    {
        private readonly ITestDataStorage _testDataStorage;
        [ObservableProperty]
        private string _testDataText = string.Empty;

        public TestDataViewModel(ITestDataStorage testDataStorage)
        {
            _testDataStorage = testDataStorage;
        }

        private bool _isInitialized = false;
        public async Task OnNavigatedToAsync()
        {
            if (_isInitialized)
                return;

            TestDataText = await _testDataStorage.LoadTestDataAsync();
            _isInitialized = true;
        }

        public async Task OnNavigatedFromAsync()
        {
            await _testDataStorage.SaveTestDataAsync(TestDataText);
        }
    }
}
