using System;
using System.Windows;
using System.Windows.Media;
using HomeworkChecker.UI.Helpers;
using HomeworkChecker.UI.Models.Settings;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Markup;

namespace HomeworkChecker.UI.Services.Settings
{
    public sealed class AppThemeService : IAppThemeService
    {
        private ThemePreference _themePreference;
        private AccentColorPreference _accentColorPreference;
        private Window? _themeWindow;

        /// <summary>
        /// 获取应用当前使用的明暗主题偏好。
        /// </summary>
        /// <returns>当前主题偏好。</returns>
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

        /// <summary>
        /// 立即应用指定的明暗主题。
        /// </summary>
        /// <param name="themePreference">待应用主题偏好。</param>
        public void Apply(ThemePreference themePreference)
        {
            _themePreference = themePreference;

            switch (themePreference)
            {
                case ThemePreference.Light:
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);
                    break;
                case ThemePreference.Dark:
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
                    break;
                default:
                    ApplicationThemeManager.ApplySystemTheme(updateAccent: false);
                    break;
            }

            ConfigureSystemThemeWatcher();
        }

        /// <summary>
        /// 立即应用系统默认主题色或用户保存的自定义主题色。
        /// </summary>
        /// <param name="preference">主题色来源。</param>
        /// <param name="customAccentColor">#RRGGBB 格式的自定义颜色。</param>
        public void ApplyAccent(AccentColorPreference preference, string customAccentColor)
        {
            _accentColorPreference = preference;

            if (preference == AccentColorPreference.Custom)
            {
                if (!ColorHelper.TryParseHex(customAccentColor, out var color))
                {
                    color = System.Windows.Media.Color.FromRgb(0, 103, 192);
                }

                ApplicationAccentColorManager.Apply(color, ApplicationThemeManager.GetAppTheme());
            }
            else
            {
                ApplicationAccentColorManager.ApplySystemAccent();
            }

            RefreshThemeResources();
            ConfigureSystemThemeWatcher();
        }

        /// <summary>
        /// 获取当前已应用到 WPF UI 资源中的强调色。
        /// </summary>
        /// <returns>当前强调色。</returns>
        public Color GetCurrentAccentColor() => ApplicationAccentColorManager.SystemAccent;

        /// <summary>
        /// 将主窗口连接到系统主题监听器，并应用当前监听策略。
        /// </summary>
        /// <param name="window">应用主窗口。</param>
        public void AttachWindow(Window window)
        {
            _themeWindow = window;
            ConfigureSystemThemeWatcher();
        }

        /// <summary>
        /// 重新载入当前主题字典，使已创建控件立即解析新的强调色画刷。
        /// </summary>
        private static void RefreshThemeResources()
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var themeDictionary = dictionaries.FirstOrDefault(dictionary =>
                dictionary.Source?.OriginalString.Contains("/Resources/Theme/", StringComparison.OrdinalIgnoreCase) == true);
            if (themeDictionary is null)
            {
                return;
            }

            dictionaries[dictionaries.IndexOf(themeDictionary)] = new ThemesDictionary
            {
                Theme = ApplicationThemeManager.GetAppTheme()
            };
        }

        /// <summary>
        /// 仅在跟随系统主题时监听窗口；自定义强调色不会被系统强调色覆盖。
        /// </summary>
        private void ConfigureSystemThemeWatcher()
        {
            if (_themeWindow is null)
            {
                return;
            }

            SystemThemeWatcher.UnWatch(_themeWindow);
            if (_themePreference == ThemePreference.System)
            {
                SystemThemeWatcher.Watch(
                    _themeWindow,
                    WindowBackdropType.Mica,
                    updateAccents: _accentColorPreference == AccentColorPreference.Default);
            }
        }
    }
}
