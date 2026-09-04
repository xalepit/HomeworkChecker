using HomeworkChecker.UI.Helpers;
using HomeworkChecker.UI.Models.Settings;
using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.Services.Settings;
using System.Windows.Media;

namespace HomeworkChecker.UI.ViewModels.Pages.Settings
{
    /// <summary>
    /// 管理主题、主题色、界面缩放和语言等个性化设置。
    /// </summary>
    public partial class PersonalizationSettingsViewModel : ObservableObject
    {
        private readonly IAppThemeService _themeService;
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IUiScaleService _uiScaleService;
        private bool _isInitializing;
        private bool _isApplyingCustomAccentColor;

        /// <summary>
        /// 创建个性化设置 ViewModel。
        /// </summary>
        /// <param name="themeService">主题和主题色服务。</param>
        /// <param name="settingsService">设置持久化服务。</param>
        /// <param name="localizationService">界面本地化服务。</param>
        /// <param name="uiScaleService">界面缩放服务。</param>
        public PersonalizationSettingsViewModel(
            IAppThemeService themeService,
            ISettingsService settingsService,
            ILocalizationService localizationService,
            IUiScaleService uiScaleService)
        {
            _themeService = themeService;
            _settingsService = settingsService;
            _localizationService = localizationService;
            _uiScaleService = uiScaleService;
        }

        /// <summary>
        /// 从持久化设置初始化全部个性化选项，期间不重复保存或应用设置。
        /// </summary>
        public void Initialize()
        {
            _isInitializing = true;

            var settings = _settingsService.GetCurrent();
            SelectedThemePreference = settings.ThemePreference;
            CustomAccentColor = NormalizeAccentColor(settings.CustomAccentColor);
            SelectedAccentColorPreference = settings.AccentColorPreference;
            SelectedUiScalePreference = settings.UiScalePreference;
            SelectedLanguagePreference = settings.LanguagePreference;

            _isInitializing = false;
            RefreshAccentPreview();
        }

        [ObservableProperty]
        private ThemePreference _selectedThemePreference;

        public string CurrentThemeText =>
            SelectedThemePreference switch
            {
                ThemePreference.Light => Translations.Settings_Theme_Light,
                ThemePreference.Dark => Translations.Settings_Theme_Dark,
                _ => Translations.Settings_UseSystem
            };

        /// <summary>
        /// 主题偏好变化后立即应用并持久化，同时恢复当前主题色。
        /// </summary>
        /// <param name="value">新的主题偏好。</param>
        partial void OnSelectedThemePreferenceChanged(ThemePreference value)
        {
            OnPropertyChanged(nameof(CurrentThemeText));
            if (_isInitializing)
            {
                return;
            }

            _themeService.Apply(value);
            _themeService.ApplyAccent(SelectedAccentColorPreference, CustomAccentColor);
            RefreshAccentPreview();
            _settingsService.Update(settings => settings.ThemePreference = value);
        }

        [ObservableProperty]
        private AccentColorPreference _selectedAccentColorPreference;

        [ObservableProperty]
        private string _customAccentColor = "#0067C0";

        [ObservableProperty]
        private SolidColorBrush _currentAccentColorBrush = new(Colors.Transparent);

        public bool IsCustomAccentColorSelected =>
            SelectedAccentColorPreference == AccentColorPreference.Custom;

        public string CurrentAccentColorText =>
            SelectedAccentColorPreference == AccentColorPreference.Default
                ? Translations.Settings_Accent_Default
                : Translations.Settings_Accent_Custom;

        public string CurrentAccentColorHex => ColorHelper.ToHex(CurrentAccentColorBrush.Color);

        /// <summary>
        /// 主题色来源变化后立即应用对应颜色并持久化。
        /// </summary>
        /// <param name="value">新的主题色来源。</param>
        partial void OnSelectedAccentColorPreferenceChanged(AccentColorPreference value)
        {
            OnPropertyChanged(nameof(CurrentAccentColorText));
            OnPropertyChanged(nameof(IsCustomAccentColorSelected));

            if (_isInitializing || _isApplyingCustomAccentColor)
            {
                return;
            }

            _themeService.ApplyAccent(value, CustomAccentColor);
            RefreshAccentPreview();
            _settingsService.Update(settings => settings.AccentColorPreference = value);
        }

        /// <summary>
        /// 保存并立即应用调色盘确认的自定义主题色。
        /// </summary>
        /// <param name="hexColor">#RRGGBB 格式的颜色。</param>
        public void ApplyCustomAccentColor(string hexColor)
        {
            CustomAccentColor = NormalizeAccentColor(hexColor);
            _isApplyingCustomAccentColor = true;
            SelectedAccentColorPreference = AccentColorPreference.Custom;
            _isApplyingCustomAccentColor = false;
            OnPropertyChanged(nameof(IsCustomAccentColorSelected));
            OnPropertyChanged(nameof(CurrentAccentColorText));

            _themeService.ApplyAccent(AccentColorPreference.Custom, CustomAccentColor);
            RefreshAccentPreview();
            _settingsService.Update(settings =>
            {
                settings.AccentColorPreference = AccentColorPreference.Custom;
                settings.CustomAccentColor = CustomAccentColor;
            });
        }

        [ObservableProperty]
        private UiScalePreference _selectedUiScalePreference;

        public string CurrentUiScaleText =>
            SelectedUiScalePreference switch
            {
                UiScalePreference.Percent100 => "100%",
                UiScalePreference.Percent125 => "125%",
                UiScalePreference.Percent150 => "150%",
                UiScalePreference.Percent175 => "175%",
                UiScalePreference.Percent200 => "200%",
                _ => Translations.Settings_UseSystem
            };

        /// <summary>
        /// 缩放偏好变化后立即缩放现有窗口并持久化。
        /// </summary>
        /// <param name="value">新的缩放偏好。</param>
        partial void OnSelectedUiScalePreferenceChanged(UiScalePreference value)
        {
            OnPropertyChanged(nameof(CurrentUiScaleText));
            if (_isInitializing)
            {
                return;
            }

            _uiScaleService.Apply(value);
            _settingsService.Update(settings => settings.UiScalePreference = value);
        }

        [ObservableProperty]
        private LanguagePreference _selectedLanguagePreference;

        /// <summary>
        /// 语言偏好变化后立即刷新本地化资源并持久化。
        /// </summary>
        /// <param name="value">新的语言偏好。</param>
        partial void OnSelectedLanguagePreferenceChanged(LanguagePreference value)
        {
            if (_isInitializing)
            {
                return;
            }

            _localizationService.Apply(value);
            _settingsService.Update(settings => settings.LanguagePreference = value);

            OnPropertyChanged(nameof(CurrentThemeText));
            OnPropertyChanged(nameof(CurrentAccentColorText));
            OnPropertyChanged(nameof(CurrentUiScaleText));
        }

        /// <summary>
        /// 从主题资源读取实际生效的强调色并刷新设置页色块。
        /// </summary>
        private void RefreshAccentPreview()
        {
            CurrentAccentColorBrush = new SolidColorBrush(_themeService.GetCurrentAccentColor());
            OnPropertyChanged(nameof(CurrentAccentColorHex));
        }

        /// <summary>
        /// 规范化持久化颜色；无效值回落到 Windows 默认蓝色。
        /// </summary>
        /// <param name="hexColor">待规范化颜色文本。</param>
        /// <returns>有效的大写 #RRGGBB 文本。</returns>
        private static string NormalizeAccentColor(string? hexColor) =>
            ColorHelper.TryParseHex(hexColor, out var color)
                ? ColorHelper.ToHex(color)
                : "#0067C0";
    }
}
