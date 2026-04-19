using System;
using System.Collections.Generic;
using System.Text;
using HomeworkChecker.UI.Models.Settings;
using Wpf.Ui.Appearance;

namespace HomeworkChecker.UI.Services.Settings
{
    public sealed class AppThemeService : IAppThemeService
    {
        public ThemePreference GetThemePreference()
        {
            var appTheme = ApplicationThemeManager.GetAppTheme();

            return appTheme switch
            {
                ApplicationTheme.Light => ThemePreference.Light,
                ApplicationTheme.Dark => ThemePreference.Dark,
                _ => ThemePreference.System
            };
        }

        public void Apply(ThemePreference themePreference)
        {
            switch (themePreference)
            {
                case ThemePreference.Light:
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    break;
                case ThemePreference.Dark:
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    break;
                default:
                    ApplicationThemeManager.ApplySystemTheme();
                    break;
            }
        }
    }
}
