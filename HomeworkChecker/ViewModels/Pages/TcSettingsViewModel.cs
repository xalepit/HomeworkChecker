using HomeworkChecker.Core.Models;
using HomeworkChecker.UI.Services.Settings;
using HomeworkChecker.UI.Resources;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    public partial class TcSettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly ISettingsService _settingsService;
        private CompareOptions _compareSettings = new();
        private bool _isInitialized;

        public TrimType TrimMode
        {
            get => _compareSettings.TrimMode;
            set => UpdateOption(_compareSettings.TrimMode, value, option => _compareSettings.TrimMode = option, nameof(TrimMode));
        }

        public int LineSkip
        {
            get => _compareSettings.LineSkip;
            set => UpdateOption(_compareSettings.LineSkip, value, option => _compareSettings.LineSkip = option, nameof(LineSkip));
        }

        public int LineOffset
        {
            get => _compareSettings.LineOffset;
            set => UpdateOption(_compareSettings.LineOffset, value, option => _compareSettings.LineOffset = option, nameof(LineOffset));
        }

        public bool IgnoreBlank
        {
            get => _compareSettings.IgnoreBlank;
            set
            {
                UpdateOption(_compareSettings.IgnoreBlank, value, option => _compareSettings.IgnoreBlank = option, nameof(IgnoreBlank));
                OnPropertyChanged(nameof(IgnoreBlankStateText));
            }
        }

        public string IgnoreBlankStateText =>
            IgnoreBlank ? Translations.Common_On : Translations.Common_Off;

        public bool CrCrLfNotEqual
        {
            get => _compareSettings.CrCrLfNotEqual;
            set
            {
                UpdateOption(_compareSettings.CrCrLfNotEqual, value, option => _compareSettings.CrCrLfNotEqual = option, nameof(CrCrLfNotEqual));
                OnPropertyChanged(nameof(LineEndingStateText));
            }
        }

        public string LineEndingStateText =>
            CrCrLfNotEqual
                ? Translations.Compare_LineEnding_Strict
                : Translations.Compare_LineEnding_Ignore;

        public int MaxDiffCount
        {
            get => _compareSettings.MaxDiffCount;
            set => UpdateOption(_compareSettings.MaxDiffCount, value, option => _compareSettings.MaxDiffCount = option, nameof(MaxDiffCount));
        }

        public int MaxLineCount
        {
            get => _compareSettings.MaxLineCount;
            set => UpdateOption(_compareSettings.MaxLineCount, value, option => _compareSettings.MaxLineCount = option, nameof(MaxLineCount));
        }

        /// <summary>
        /// 初始化文本比较设置 ViewModel。
        /// </summary>
        /// <param name="settingsService">应用设置持久化服务。</param>
        public TcSettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            Translations.CultureChanged += OnCultureChanged;
        }

        /// <summary>
        /// 首次进入页面时加载已经保存的文本比较设置。
        /// </summary>
        /// <returns>已完成的异步任务。</returns>
        public Task OnNavigatedToAsync()
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            _compareSettings = _settingsService.GetCurrent().TextComparisonOptions ?? new CompareOptions();
            _isInitialized = true;
            NotifyAllOptionProperties();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 离开页面时再次保存当前文本比较设置。
        /// </summary>
        /// <returns>已完成的异步任务。</returns>
        public Task OnNavigatedFromAsync()
        {
            PersistCompareSettings();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 更新一个比较选项并立即保存，避免关闭当前页面时丢失修改。
        /// </summary>
        /// <typeparam name="T">比较选项的值类型。</typeparam>
        /// <param name="currentValue">当前值。</param>
        /// <param name="newValue">待应用的新值。</param>
        /// <param name="applyValue">将新值写入 CompareOptions 的操作。</param>
        /// <param name="propertyName">需要通知界面刷新的属性名。</param>
        private void UpdateOption<T>(T currentValue, T newValue, Action<T> applyValue, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                return;
            }

            applyValue(newValue);
            OnPropertyChanged(propertyName);
            PersistCompareSettings();
        }

        /// <summary>
        /// 将当前比较设置写入统一的应用设置文件。
        /// </summary>
        private void PersistCompareSettings()
        {
            if (!_isInitialized)
            {
                return;
            }

            _settingsService.Update(settings => settings.TextComparisonOptions = _compareSettings);
        }

        /// <summary>
        /// 加载设置对象后通知界面刷新全部比较选项。
        /// </summary>
        private void NotifyAllOptionProperties()
        {
            OnPropertyChanged(nameof(TrimMode));
            OnPropertyChanged(nameof(LineSkip));
            OnPropertyChanged(nameof(LineOffset));
            OnPropertyChanged(nameof(IgnoreBlank));
            OnPropertyChanged(nameof(IgnoreBlankStateText));
            OnPropertyChanged(nameof(CrCrLfNotEqual));
            OnPropertyChanged(nameof(LineEndingStateText));
            OnPropertyChanged(nameof(MaxDiffCount));
            OnPropertyChanged(nameof(MaxLineCount));
        }

        /// <summary>
        /// 语言切换后刷新两个开关旁的状态文本。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IgnoreBlankStateText));
            OnPropertyChanged(nameof(LineEndingStateText));
        }
    }
}
