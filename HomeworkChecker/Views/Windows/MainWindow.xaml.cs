using HomeworkChecker.UI.ViewModels.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace HomeworkChecker.UI.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        /// <summary>
        /// 创建主窗口并连接导航、对话框与消息提示服务。
        /// </summary>
        /// <param name="viewModel">主窗口 ViewModel。</param>
        /// <param name="navigationViewPageProvider">导航页面实例提供服务。</param>
        /// <param name="navigationService">应用导航服务。</param>
        /// <param name="contentDialogService">内容对话框服务。</param>
        /// <param name="snackbarService">消息提示服务。</param>
        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IContentDialogService contentDialogService,
            ISnackbarService snackbarService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);
            contentDialogService.SetDialogHost(RootContentDialog);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }

        #region INavigationWindow methods

        /// <summary>
        /// 获取主窗口使用的导航控件。
        /// </summary>
        /// <returns>当前窗口的导航控件。</returns>
        public INavigationView GetNavigation() => RootNavigation;

        /// <summary>
        /// 导航到指定页面类型。
        /// </summary>
        /// <param name="pageType">目标页面类型。</param>
        /// <returns>导航成功时返回 <see langword="true"/>。</returns>
        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        /// <summary>
        /// 设置由依赖注入容器提供页面实例的页面服务。
        /// </summary>
        /// <param name="navigationViewPageProvider">页面实例提供服务。</param>
        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        /// <summary>
        /// 将依赖注入服务提供器附加到导航控件。
        /// </summary>
        /// <param name="serviceProvider">应用服务提供器。</param>
        public void SetServiceProvider(IServiceProvider serviceProvider) => RootNavigation.SetServiceProvider(serviceProvider);

        /// <summary>
        /// 点击当前输入控件以外的区域时清除键盘焦点，结束 NumberBox 编辑状态。
        /// </summary>
        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var focusedElement = Keyboard.FocusedElement as DependencyObject;
            var clickedElement = e.OriginalSource as DependencyObject;
            var focusedNumberBox = FindAncestor<NumberBox>(focusedElement);
            if (focusedNumberBox is null || clickedElement is null || IsDescendantOf(clickedElement, focusedNumberBox))
            {
                return;
            }

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(focusedNumberBox), null);
            Keyboard.ClearFocus();
        }

        /// <summary>
        /// 在可视化树中查找元素自身或最近的指定类型祖先。
        /// </summary>
        /// <typeparam name="T">需要查找的控件类型。</typeparam>
        /// <param name="element">查找起点。</param>
        /// <returns>找到的控件；不存在时返回 <see langword="null"/>。</returns>
        private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
        {
            for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
            }

            return null;
        }

        /// <summary>
        /// 判断元素是否位于指定祖先的可视化树内。
        /// </summary>
        /// <param name="element">待检查元素。</param>
        /// <param name="ancestor">候选祖先。</param>
        /// <returns>位于祖先内部时返回 <see langword="true"/>。</returns>
        private static bool IsDescendantOf(DependencyObject element, DependencyObject ancestor)
        {
            for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 显示主窗口。
        /// </summary>
        public void ShowWindow() => Show();

        /// <summary>
        /// 关闭主窗口。
        /// </summary>
        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// 在主窗口关闭后结束应用程序。
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 主窗口关闭后同步结束应用，避免后台宿主继续运行。
            Application.Current.Shutdown();
        }
    }
}
