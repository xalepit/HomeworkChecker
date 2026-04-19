using HomeworkChecker.UI.Models.Settings;
using Wpf.Ui;

namespace HomeworkChecker.UI.Services.Settings
{
    public interface IAppThemeService
    {
        ThemePreference GetThemePreference();
        void Apply(ThemePreference themePreference);
    }
}
