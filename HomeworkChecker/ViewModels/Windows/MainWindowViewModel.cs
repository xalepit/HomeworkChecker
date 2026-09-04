using System.Collections.ObjectModel;
using HomeworkChecker.UI.Resources;
using Wpf.Ui.Controls;

namespace HomeworkChecker.UI.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly NavigationViewItem _homeMenuItem;
        private readonly NavigationViewItem _testDataMenuItem;
        private readonly NavigationViewItem _compareSettingsMenuItem;
        private readonly NavigationViewItem _settingsMenuItem;
        private readonly MenuItem _trayHomeMenuItem;

        [ObservableProperty]
        private string _applicationTitle = App.AppName;

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new();

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new();

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new();

        /// <summary>
        /// 创建主窗口导航项，并监听语言切换以刷新导航文本。
        /// </summary>
        public MainWindowViewModel()
        {
            _homeMenuItem = new NavigationViewItem
            {
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.HomePage)
            };
            _testDataMenuItem = new NavigationViewItem
            {
                Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardTextEdit24 },
                TargetPageType = typeof(Views.Pages.TestDataPage)
            };
            _compareSettingsMenuItem = new NavigationViewItem
            {
                Icon = new SymbolIcon { Symbol = SymbolRegular.TextBoxSettings24 },
                TargetPageType = typeof(Views.Pages.TcSettingsPage)
            };
            _settingsMenuItem = new NavigationViewItem
            {
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            };
            _trayHomeMenuItem = new MenuItem { Tag = "tray_home" };

            MenuItems.Add(_homeMenuItem);
            MenuItems.Add(_testDataMenuItem);
            MenuItems.Add(_compareSettingsMenuItem);
            FooterMenuItems.Add(_settingsMenuItem);
            TrayMenuItems.Add(_trayHomeMenuItem);

            RefreshLocalizedText();
            Translations.CultureChanged += OnCultureChanged;
        }

        /// <summary>
        /// 在语言切换后刷新所有导航项和托盘菜单文本。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnCultureChanged(object? sender, EventArgs e) => RefreshLocalizedText();

        /// <summary>
        /// 将当前语言对应的实际字符串写入导航控件，避免框架显示绑定对象类型名。
        /// </summary>
        private void RefreshLocalizedText()
        {
            _homeMenuItem.Content = Translations.Current["Navigation_Home"];
            _testDataMenuItem.Content = Translations.Current["Navigation_TestData"];
            _compareSettingsMenuItem.Content = Translations.Current["Navigation_CompareSettings"];
            _settingsMenuItem.Content = Translations.Current["Navigation_Settings"];
            _trayHomeMenuItem.Header = Translations.Current["Navigation_Home"];
        }
    }
}
