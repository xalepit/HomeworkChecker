using System.Globalization;
using HomeworkChecker.UI.Models.Settings;
using HomeworkChecker.UI.Resources;

namespace HomeworkChecker.UI.Services.Settings
{
    public class LocalizationService : ILocalizationService
    {
        public void Apply(LanguagePreference preference)
        {
            // 枚举 -> 标准 culture name
            var cultureName = preference switch
            {
                LanguagePreference.zh_CN => "zh-CN",
                LanguagePreference.zh_TW => "zh-TW",
                LanguagePreference.en_US => "en-US",
                _ => "zh-CN"
            };

            var culture = new CultureInfo(cultureName);
            Translations.SetCulture(culture);

            // 设置默认线程文化（后续创建线程生效）
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // 当前线程立即生效
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
