using HomeworkChecker.Core.Models;

namespace HomeworkChecker.UI.Models.Settings
{
    /// <summary>
    /// 保存应用程序的通用设置和文本比较设置。
    /// </summary>
    public sealed class AppSettings
    {
        public ThemePreference ThemePreference { get; set; }
        public AccentColorPreference AccentColorPreference { get; set; }
        public string CustomAccentColor { get; set; } = "#0067C0";
        public UiScalePreference UiScalePreference { get; set; }
        public LanguagePreference LanguagePreference { get; set; }
        public int ExecutionTimeoutSeconds { get; set; } = 5;
        public int MaxParallelism { get; set; } = 4;
        public string LastDemoExePath { get; set; } = string.Empty;
        public string LastStudentExePath { get; set; } = string.Empty;
        public CompareOptions TextComparisonOptions { get; set; } = new();
    }
}
