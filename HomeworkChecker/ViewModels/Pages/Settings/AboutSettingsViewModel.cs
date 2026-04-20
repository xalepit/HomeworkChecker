namespace HomeworkChecker.UI.ViewModels.Pages.Settings
{
    public partial class AboutSettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _appVersion = string.Empty;

        public void Initialize() => AppVersion = $"{App.AppName} - {GetAssemblyVersion()}";

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

    }
}
