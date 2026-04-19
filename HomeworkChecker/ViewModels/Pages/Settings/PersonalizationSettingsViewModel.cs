using HomeworkChecker.UI.Models.Settings;
using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.Services.Settings;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages.Settings
{
    public partial class PersonalizationSettingsViewModel : ObservableObject
    {
        private readonly IAppThemeService _themeService;
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly ISnackbarService _snackbarService;
        private bool _isInitializing;

        public PersonalizationSettingsViewModel(
            IAppThemeService themeService,
            ISettingsService settingsService,
            ILocalizationService localizationService,
            ISnackbarService snackbarService)
        {
            _themeService = themeService;
            _settingsService = settingsService;
            _localizationService = localizationService;
            _snackbarService = snackbarService;
        }
        public void Initialize()
        {
            _isInitializing = true;

            var settings = _settingsService.GetCurrent();

            SelectedThemePreference = settings.ThemePreference;
            SelectedAccentColorPreference = settings.AccentColorPreference;
            SelectedUiScalePreference = settings.UiScalePreference;
            SelectedLanguagePreference = settings.LanguagePreference;

            IsScaleRestartRequired = false;
            IsLanguageRestartRequired = false;

            _isInitializing = false;
        }
        private void ShowRestartRequiredSnackbar()
        {
            _snackbarService.Show(
                Translations.Snackbar_Saved_Title,
                Translations.Snackbar_Restart_Message,
                ControlAppearance.Success,
                new SymbolIcon { Symbol = SymbolRegular.CheckmarkCircle24 },
                TimeSpan.FromSeconds(2)
            );
        }
        // 应用主题相关设置
        [ObservableProperty]
        private ThemePreference _selectedThemePreference;

        public string CurrentThemeText =>
            SelectedThemePreference switch
            {
                ThemePreference.Light => Translations.Settings_Theme_Light,
                ThemePreference.Dark => Translations.Settings_Theme_Dark,
                _ => Translations.Settings_UseSystem
            };

        //?
        partial void OnSelectedThemePreferenceChanged(ThemePreference value)
        {
            // 让 Header 上的中文文本刷新
            OnPropertyChanged(nameof(CurrentThemeText));
            if (_isInitializing) return;

            _themeService.Apply(value);
            _settingsService.Update(s => s.ThemePreference = value);
        }

        // 主题颜色相关设置
        [ObservableProperty]
        private AccentColorPreference _selectedAccentColorPreference;

        public bool IsCustomAccentColorSelected =>
            SelectedAccentColorPreference == AccentColorPreference.Custom;
        public string CurrentAccentColorText =>
            SelectedAccentColorPreference == AccentColorPreference.Default
                ? Translations.Settings_Accent_Default
                : Translations.Settings_Accent_Custom;
        // 主题色模式切换时，刷新依赖属性
        partial void OnSelectedAccentColorPreferenceChanged(AccentColorPreference value)
        {
            OnPropertyChanged(nameof(CurrentAccentColorText));
            OnPropertyChanged(nameof(IsCustomAccentColorSelected));

            if (_isInitializing) return;

            _settingsService.Update(s => s.AccentColorPreference = value);
        }

        // 界面缩放相关设置
        [ObservableProperty]
        private bool _isScaleRestartRequired;

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
        partial void OnSelectedUiScalePreferenceChanged(UiScalePreference value)
        {
            OnPropertyChanged(nameof(CurrentUiScaleText));
            if (_isInitializing) return;

            _settingsService.Update(s => s.UiScalePreference = value);
            IsScaleRestartRequired = true;
            ShowRestartRequiredSnackbar();
        }

        // 语言相关设置
        [ObservableProperty]
        private bool _isLanguageRestartRequired;

        [ObservableProperty]
        private LanguagePreference _selectedLanguagePreference;
        partial void OnSelectedLanguagePreferenceChanged(LanguagePreference value)
        {

            if (_isInitializing) return;

            _localizationService.Apply(value);
            _settingsService.Update(s => s.LanguagePreference = value);
            IsLanguageRestartRequired = true;
            ShowRestartRequiredSnackbar();

            OnPropertyChanged(nameof(CurrentThemeText));
            OnPropertyChanged(nameof(CurrentAccentColorText));
            OnPropertyChanged(nameof(CurrentUiScaleText));
        }
    }
}
