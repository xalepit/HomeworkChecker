using System;
using System.Collections.Generic;
using System.Text;

namespace HomeworkChecker.UI.Models.Settings
{
    // 应用设置总模型（后续可继续扩展）
    public sealed class AppSettings
    {
        public ThemePreference ThemePreference { get; set; }
        public AccentColorPreference AccentColorPreference { get; set; } 
        public UiScalePreference UiScalePreference { get; set; }
        public LanguagePreference LanguagePreference { get; set; }
    }

}
