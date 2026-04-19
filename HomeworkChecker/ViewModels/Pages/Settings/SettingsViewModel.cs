using HomeworkChecker.UI.ViewModels.Pages.Settings;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        public PersonalizationSettingsViewModel PersonalizationSettings { get; }
        public AboutSettingsViewModel AboutSettings { get; }

        public SettingsViewModel(PersonalizationSettingsViewModel peronalizationSettings, AboutSettingsViewModel aboutSettings)
        {
            PersonalizationSettings = peronalizationSettings;
            AboutSettings = aboutSettings;
        }
        public Task OnNavigatedToAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            PersonalizationSettings.Initialize();
            AboutSettings.Initialize();

            _isInitialized = true;
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;
    }
}
