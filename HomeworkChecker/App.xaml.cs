using HomeworkChecker.Core.Services;
using HomeworkChecker.UI.Services;
using HomeworkChecker.UI.Services.Settings;
using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.ViewModels.Pages;
using HomeworkChecker.UI.ViewModels.Windows;
using HomeworkChecker.UI.Views.Pages;
using HomeworkChecker.UI.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace HomeworkChecker.UI
{
    /// <summary>
    /// 负责创建应用宿主、注册依赖并管理应用生命周期。
    /// </summary>
    public partial class App
    {
        public const string AppName = "HomeworkChecker";

        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(configuration =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                services.AddSingleton<Services.Settings.IAppThemeService, Services.Settings.AppThemeService>();
                services.AddSingleton<ViewModels.Pages.Settings.PersonalizationSettingsViewModel>();
                services.AddSingleton<ViewModels.Pages.Settings.AboutSettingsViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<Services.Settings.ISettingsService, Services.Settings.JsonSettingsService>();
                services.AddSingleton<Services.Settings.IUiScaleService, Services.Settings.UiScaleService>();
                services.AddSingleton<Services.Settings.ILocalizationService, Services.Settings.LocalizationService>();

                services.AddSingleton<HomePage>();
                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<TcSettingsPage>();
                services.AddSingleton<TcSettingsViewModel>();
                services.AddSingleton<TestDataPage>();
                services.AddSingleton<TestDataViewModel>();

                services.AddSingleton<ITestDataStorage, TestDataStorageService>();
                services.AddSingleton<IFilePickerService, FilePickerService>();
                services.AddSingleton<TestDataParser>();
                services.AddSingleton<BatchComparer>();
            })
            .Build();

        /// <summary>
        /// 获取应用依赖注入容器。
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// 应用启动时先应用语言设置，再启动通用宿主。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">启动事件参数。</param>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            // 在 Host 启动前优先应用语言，确保 XAML 静态资源读取到正确文化
            var earlySettingsService = new JsonSettingsService();
            var earlyLocalizationService = new LocalizationService();
            var earlySettings = earlySettingsService.GetCurrent();
            earlyLocalizationService.Apply(earlySettings.LanguagePreference);

            await _host.StartAsync();
        }

        /// <summary>
        /// 应用退出时保存已编辑测试数据，并停止和释放通用宿主。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">退出事件参数。</param>
        private void OnExit(object sender, ExitEventArgs e)
        {
            var homeViewModel = _host.Services.GetService<HomeViewModel>();
            var testDataViewModel = _host.Services.GetService<TestDataViewModel>();
            try
            {
                if (homeViewModel is not null)
                {
                    homeViewModel.CancelActiveRunAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    exception.Message,
                    Translations.Current["Home_RunFailed"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            try
            {
                if (testDataViewModel is not null)
                {
                    testDataViewModel.SaveIfInitializedAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"{Translations.TestData_SaveFailed}\n{exception.Message}",
                    AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            _host.StopAsync().GetAwaiter().GetResult();

            _host.Dispose();
        }
    }
}
