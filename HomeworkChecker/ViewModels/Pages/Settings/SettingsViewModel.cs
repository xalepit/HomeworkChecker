using HomeworkChecker.UI.Services.Settings;
using HomeworkChecker.UI.ViewModels.Pages.Settings;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.ViewModels.Pages
{
    /// <summary>
    /// 汇总通用设置页面中的个性化、运行和关于设置。
    /// </summary>
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly ISettingsService _settingsService;
        private bool _isInitialized;
        private bool _isInitializing;

        public PersonalizationSettingsViewModel PersonalizationSettings { get; }
        public AboutSettingsViewModel AboutSettings { get; }

        [ObservableProperty]
        private double _executionTimeoutSeconds = 5;

        [ObservableProperty]
        private double _maxParallelism = 4;

        /// <summary>
        /// 创建通用设置页面 ViewModel。
        /// </summary>
        /// <param name="personalizationSettings">个性化设置 ViewModel。</param>
        /// <param name="aboutSettings">关于页面设置 ViewModel。</param>
        /// <param name="settingsService">设置持久化服务。</param>
        public SettingsViewModel(
            PersonalizationSettingsViewModel personalizationSettings,
            AboutSettingsViewModel aboutSettings,
            ISettingsService settingsService)
        {
            PersonalizationSettings = personalizationSettings;
            AboutSettings = aboutSettings;
            _settingsService = settingsService;
        }

        /// <summary>
        /// 首次进入页面时恢复全部通用设置。
        /// </summary>
        public Task OnNavigatedToAsync()
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            _isInitializing = true;
            PersonalizationSettings.Initialize();
            AboutSettings.Initialize();

            var settings = _settingsService.GetCurrent();
            var normalizedTimeout = Math.Clamp(settings.ExecutionTimeoutSeconds, 3, 10);
            ExecutionTimeoutSeconds = normalizedTimeout;
            if (settings.ExecutionTimeoutSeconds != normalizedTimeout)
            {
                _settingsService.Update(value => value.ExecutionTimeoutSeconds = normalizedTimeout);
            }

            var normalizedParallelism = Math.Clamp(settings.MaxParallelism, 1, 8);
            MaxParallelism = normalizedParallelism;
            if (settings.MaxParallelism != normalizedParallelism)
            {
                _settingsService.Update(value => value.MaxParallelism = normalizedParallelism);
            }

            _isInitializing = false;
            _isInitialized = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 离开设置页面时无需额外处理，设置已在修改时保存。
        /// </summary>
        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// 超时时间变化后约束为 3–10 秒并立即持久化。
        /// </summary>
        /// <param name="value">NumberBox 提供的新值。</param>
        partial void OnExecutionTimeoutSecondsChanged(double value)
        {
            var normalizedTimeout = double.IsFinite(value)
                ? Math.Clamp((int)Math.Round(value), 3, 10)
                : 5;

            if (value != normalizedTimeout)
            {
                ExecutionTimeoutSeconds = normalizedTimeout;
                return;
            }

            if (!_isInitializing)
            {
                _settingsService.Update(settings => settings.ExecutionTimeoutSeconds = normalizedTimeout);
            }
        }

        /// <summary>
        /// 并行数变化后约束为 1–8 并立即持久化。
        /// </summary>
        /// <param name="value">NumberBox 提供的新值。</param>
        partial void OnMaxParallelismChanged(double value)
        {
            var normalizedParallelism = double.IsFinite(value)
                ? Math.Clamp((int)Math.Round(value), 1, 8)
                : 4;

            if (value != normalizedParallelism)
            {
                MaxParallelism = normalizedParallelism;
                return;
            }

            if (!_isInitializing)
            {
                _settingsService.Update(settings => settings.MaxParallelism = normalizedParallelism);
            }
        }
    }
}
