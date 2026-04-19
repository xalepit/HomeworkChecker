using HomeworkChecker.UI.Services;
using HomeworkChecker.UI.Services.Settings;
using HomeworkChecker.UI.ViewModels.Pages;
using HomeworkChecker.UI.ViewModels.Windows;
using HomeworkChecker.UI.Views.Pages;
using HomeworkChecker.UI.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace HomeworkChecker.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging

        public const string AppName = "Common";

        private static readonly IHost _host = Host
            .CreateDefaultBuilder() //创建默认宿主构建器IHostBuilder，自动准备一套常见基础能力（配置、日志、环境等默认行为）
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); }) //配置“配置系统”的读取根路径（一般不用改）
            .ConfigureServices((context, services) => //往DI容器里注册依赖
            {
                services.AddNavigationViewPageProvider(); //为WPF UI NavigationView 添加其页面导航(page navigation)必要的服务

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // 各个（/Views/）Page(页面)和ViewModel（页面后的逻辑）
                // 之后如果有新的页面，都要在这添加服务
                // Settings
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
            }).Build(); //Build创建IHost实例，这里赋值给私有只读字段_host

        //A dependency is an object that another object depends on.

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            // 在 Host 启动前优先应用语言，确保 XAML 静态资源读取到正确文化
            var earlySettingsService = new JsonSettingsService();
            var earlyLocalizationService = new LocalizationService();
            var earlySettings = earlySettingsService.GetCurrent();
            earlyLocalizationService.Apply(earlySettings.LanguagePreference);

            await _host.StartAsync();
        }
        //程序启动时，启动 Host。

        //这一步会触发：

        //Host 启动
        //HostedService 启动
        //ApplicationHostService 开始执行

        //于是主窗口、导航等通常就在这里被带起来。


        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }//程序退出时：

        //先正常停止 Host
        //再释放资源

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
        //这是全局 UI 线程未处理异常事件。

        //        以后你可以在这里做：

        //统一弹错误框
        //写日志
        //防止程序直接崩掉

        //现在模板先留空。
    }
}
