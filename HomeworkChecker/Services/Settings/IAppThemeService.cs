using HomeworkChecker.UI.Models.Settings;
using System.Windows;
using System.Windows.Media;

namespace HomeworkChecker.UI.Services.Settings
{
    public interface IAppThemeService
    {
        /// <summary>
        /// 获取应用当前使用的明暗主题偏好。
        /// </summary>
        /// <returns>当前主题偏好。</returns>
        ThemePreference GetThemePreference();

        /// <summary>
        /// 立即应用指定的明暗主题。
        /// </summary>
        /// <param name="themePreference">待应用主题偏好。</param>
        void Apply(ThemePreference themePreference);

        /// <summary>
        /// 立即应用默认或自定义主题色。
        /// </summary>
        /// <param name="preference">主题色来源。</param>
        /// <param name="customAccentColor">#RRGGBB 格式的自定义颜色。</param>
        void ApplyAccent(AccentColorPreference preference, string customAccentColor);

        /// <summary>
        /// 获取当前已应用到 WPF UI 资源中的强调色。
        /// </summary>
        /// <returns>当前强调色。</returns>
        Color GetCurrentAccentColor();

        /// <summary>
        /// 将主窗口连接到系统主题监听器，并按当前偏好决定是否同步系统强调色。
        /// </summary>
        /// <param name="window">应用主窗口。</param>
        void AttachWindow(Window window);
    }
}
