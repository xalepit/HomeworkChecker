using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.Views.Pages
{
    public partial class HomePage : INavigableView<HomeViewModel>
    {
        public HomeViewModel ViewModel { get; }

        /// <summary>
        /// 创建主页，并监听语言切换以刷新导航标题。
        /// </summary>
        /// <param name="viewModel">主页 ViewModel。</param>
        public HomePage(HomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            RefreshLocalizedTitle();
            Translations.CultureChanged += OnCultureChanged;
        }

        /// <summary>
        /// 在语言切换后刷新页面标题。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void OnCultureChanged(object? sender, EventArgs e) => RefreshLocalizedTitle();

        /// <summary>
        /// 将当前语言对应的实际字符串写入页面标题。
        /// </summary>
        private void RefreshLocalizedTitle() => Title = Translations.Current["Page_Home"];

        /// <summary>
        /// 选择用例后等待布局更新，再将详情标题滚动到可见区域。
        /// </summary>
        /// <param name="sender">结果列表。</param>
        /// <param name="e">选择变化参数。</param>
        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsList.SelectedItem is not null)
            {
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(BringDetailIntoView));
            }
        }

        /// <summary>
        /// 将详情区域滚动到当前页面的可视范围内。
        /// </summary>
        private void BringDetailIntoView() => DetailSection.BringIntoView();

        /// <summary>
        /// 横向内容使用 Shift+滚轮移动；普通滚轮转交页面纵向滚动容器。
        /// </summary>
        /// <param name="sender">当前横向滚动区域。</param>
        /// <param name="e">鼠标滚轮参数。</param>
        private void HorizontalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var viewer = (ScrollViewer)sender;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset - e.Delta);
                e.Handled = true;
                return;
            }

            var parent = FindVisualParent<ScrollViewer>(viewer);
            if (parent is null)
            {
                return;
            }

            e.Handled = true;
            parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = Mouse.MouseWheelEvent
            });
        }

        /// <summary>
        /// 查找指定类型的首个可视父元素。
        /// </summary>
        /// <typeparam name="T">需要查找的父元素类型。</typeparam>
        /// <param name="element">查找起点。</param>
        /// <returns>首个匹配父元素；不存在时返回空。</returns>
        private static T? FindVisualParent<T>(DependencyObject element)
            where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent is not null)
            {
                if (parent is T match)
                {
                    return match;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

    }
}
