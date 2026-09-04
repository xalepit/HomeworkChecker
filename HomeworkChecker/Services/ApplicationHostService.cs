using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using HomeworkChecker.UI.Models.Settings;
using HomeworkChecker.UI.Services.Settings;
using HomeworkChecker.UI.Views.Windows;
using Wpf.Ui;

namespace HomeworkChecker.UI.Services
{
    /// <summary>
    /// 管理应用启动阶段的设置应用、主窗口创建与首次导航。
    /// </summary>
    public class ApplicationHostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISettingsService _settingsService;
        private readonly IUiScaleService _uiScaleService;
        private readonly IAppThemeService _appThemeService;

        /// <summary>
        /// 创建应用宿主服务。
        /// </summary>
        /// <param name="serviceProvider">应用依赖注入容器。</param>
        /// <param name="settingsService">应用设置服务。</param>
        /// <param name="uiScaleService">界面缩放服务。</param>
        /// <param name="appThemeService">应用主题服务。</param>
        public ApplicationHostService(
            IServiceProvider serviceProvider,
            ISettingsService settingsService,
            IUiScaleService uiScaleService,
            IAppThemeService appThemeService)
        {
            _serviceProvider = serviceProvider;
            _settingsService = settingsService;
            _uiScaleService = uiScaleService;
            _appThemeService = appThemeService;
        }

        /// <summary>
        /// 宿主启动时创建并显示主窗口。
        /// </summary>
        /// <param name="cancellationToken">宿主启动取消标记。</param>
        /// <returns>窗口激活完成后的任务。</returns>
        public Task StartAsync(CancellationToken cancellationToken) => HandleActivationAsync();

        /// <summary>
        /// 宿主停止时完成服务关闭流程。
        /// </summary>
        /// <param name="cancellationToken">宿主关闭取消标记。</param>
        /// <returns>已完成的关闭任务。</returns>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// 应用激活时应用启动设置并导航至主页。
        /// </summary>
        /// <returns>已完成的激活任务。</returns>
        private Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                // 恢复上次退出前保存的个性化设置。
                var settings = _settingsService.GetCurrent();
                _appThemeService.Apply(settings.ThemePreference);
                _appThemeService.ApplyAccent(settings.AccentColorPreference, settings.CustomAccentColor);

                var navigationWindow = _serviceProvider.GetRequiredService<INavigationWindow>();
                navigationWindow.ShowWindow();

                if (navigationWindow is MainWindow mainWindow)
                {
                    _appThemeService.AttachWindow(mainWindow);
                }

                _uiScaleService.Apply(settings.UiScalePreference);

                navigationWindow.Navigate(typeof(Views.Pages.HomePage));
            }

            return Task.CompletedTask;
        }
    }
}
