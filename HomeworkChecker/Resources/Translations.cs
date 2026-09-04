using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace HomeworkChecker.UI.Resources
{
    /// <summary>
    /// 提供可在运行时刷新绑定的本地化资源访问入口。
    /// </summary>
    public sealed class Translations : INotifyPropertyChanged
    {
        private static readonly ResourceManager _resourceManager =
            new("HomeworkChecker.UI.Resources.Translations", typeof(Translations).Assembly);

        public static Translations Current { get; } = new();

        public static event EventHandler? CultureChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public static CultureInfo? Culture { get; private set; }

        public string this[string key] => Get(key);

        /// <summary>
        /// 切换当前界面文化，并通知所有本地化绑定重新读取文本。
        /// </summary>
        /// <param name="culture">新的界面文化。</param>
        public static void SetCulture(CultureInfo culture)
        {
            Culture = culture;
            CultureChanged?.Invoke(null, EventArgs.Empty);
            Current.PropertyChanged?.Invoke(Current, new PropertyChangedEventArgs("Item[]"));
        }

        /// <summary>
        /// 根据当前界面文化读取指定资源；资源缺失时返回键名以便定位。
        /// </summary>
        /// <param name="key">资源键。</param>
        /// <returns>本地化文本或原始键名。</returns>
        private static string Get(string key) =>
            _resourceManager.GetString(key, Culture ?? CultureInfo.CurrentUICulture) ?? key;

        public static string Settings_Theme_Light => Get(nameof(Settings_Theme_Light));
        public static string Settings_Theme_Dark => Get(nameof(Settings_Theme_Dark));
        public static string Settings_UseSystem => Get(nameof(Settings_UseSystem));
        public static string Settings_Accent_Default => Get(nameof(Settings_Accent_Default));
        public static string Settings_Accent_Custom => Get(nameof(Settings_Accent_Custom));
        public static string FilePicker_DemoTitle => Get(nameof(FilePicker_DemoTitle));
        public static string FilePicker_StudentTitle => Get(nameof(FilePicker_StudentTitle));
        public static string FilePicker_ExecutableFiles => Get(nameof(FilePicker_ExecutableFiles));
        public static string FilePicker_TestDataTitle => Get(nameof(FilePicker_TestDataTitle));
        public static string FilePicker_TestDataFiles => Get(nameof(FilePicker_TestDataFiles));
        public static string FilePicker_AllFiles => Get(nameof(FilePicker_AllFiles));
        public static string TestData_ImportSuccess => Get(nameof(TestData_ImportSuccess));
        public static string TestData_ImportFailed => Get(nameof(TestData_ImportFailed));
        public static string TestData_LoadFailed => Get(nameof(TestData_LoadFailed));
        public static string TestData_SaveSuccess => Get(nameof(TestData_SaveSuccess));
        public static string TestData_SaveSuccessDescription => Get(nameof(TestData_SaveSuccessDescription));
        public static string TestData_SaveFailed => Get(nameof(TestData_SaveFailed));
        public static string Settings_SaveFailed => Get(nameof(Settings_SaveFailed));
        public static string Settings_SaveFailedDescription => Get(nameof(Settings_SaveFailedDescription));
        public static string ColorPicker_Title => Get(nameof(ColorPicker_Title));
        public static string Common_Confirm => Get(nameof(Common_Confirm));
        public static string Common_Cancel => Get(nameof(Common_Cancel));
        public static string Common_On => Get(nameof(Common_On));
        public static string Common_Off => Get(nameof(Common_Off));
        public static string Compare_LineEnding_Strict => Get(nameof(Compare_LineEnding_Strict));
        public static string Compare_LineEnding_Ignore => Get(nameof(Compare_LineEnding_Ignore));
    }
}
