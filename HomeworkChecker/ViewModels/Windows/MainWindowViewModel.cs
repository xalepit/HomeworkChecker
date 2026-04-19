using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace HomeworkChecker.UI.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = $"WPF UI - {App.AppName}";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.HomePage)
            },
            new NavigationViewItem()
            {
                Content = "TestData",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ClipboardTextEdit24 },
                TargetPageType = typeof(Views.Pages.TestDataPage)
            },
            new NavigationViewItem()
            {
                Content = "txt_compare settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.TextBoxSettings24 },
                TargetPageType = typeof(Views.Pages.TcSettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}
