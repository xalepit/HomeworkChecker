using Microsoft.Extensions.Hosting;
using HomeworkChecker.UI.Models.Settings;
using HomeworkChecker.UI.Services.Settings;
using HomeworkChecker.UI.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace HomeworkChecker.UI.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IUiScaleService _uiScaleService;
        private readonly IAppThemeService _appThemeService;

        private INavigationWindow _navigationWindow;

        public ApplicationHostService(
                    IServiceProvider serviceProvider,
                    ISettingsService settingsService,
                    ILocalizationService localizationService,
                    IUiScaleService uiScaleService,
                    IAppThemeService appThemeService)
        {
            _serviceProvider = serviceProvider;
            _settingsService = settingsService;
            _localizationService = localizationService;
            _uiScaleService = uiScaleService;
            _appThemeService = appThemeService;
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private async Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                // 先应用“重启后生效”的设置
                var settings = _settingsService.GetCurrent();
                _appThemeService.Apply(settings.ThemePreference);

                _navigationWindow = (_serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow)!;
                _navigationWindow.ShowWindow();

                // 仅 System 模式启用系统主题监听
                if (settings.ThemePreference == ThemePreference.System &&
                    _navigationWindow is MainWindow mainWindow)
                {
                    SystemThemeWatcher.Watch(mainWindow);
                }

                _uiScaleService.Apply(settings.UiScalePreference);

                _navigationWindow.Navigate(typeof(Views.Pages.SettingsPage));
            }

            await Task.CompletedTask;

        }
    }
}
