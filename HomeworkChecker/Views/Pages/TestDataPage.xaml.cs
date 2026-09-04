using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.ViewModels.Pages;
using HomeworkChecker.UI.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.Views.Pages
{
    public partial class TestDataPage : INavigableView<TestDataViewModel>
    {
        public TestDataViewModel ViewModel { get; }

        /// <summary>
        /// 创建测试数据页面，并监听语言切换以刷新导航标题。
        /// </summary>
        /// <param name="viewModel">测试数据页面 ViewModel。</param>
        public TestDataPage(TestDataViewModel viewModel)
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
        private void RefreshLocalizedTitle() => Title = Translations.Current["Page_TestData"];

        /// <summary>
        /// 以模态窗口显示 stdin 与命令行参数测试数据示例。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件参数。</param>
        private void ShowExamples_Click(object sender, RoutedEventArgs e)
        {
            new TestDataExamplesWindow
            {
                Owner = Window.GetWindow(this)
            }.ShowDialog();
        }
    }
}
