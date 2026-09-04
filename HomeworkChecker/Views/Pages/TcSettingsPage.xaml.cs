using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.Views.Pages
{
    public partial class TcSettingsPage : INavigableView<TcSettingsViewModel>
    {
        public TcSettingsViewModel ViewModel { get; }

        /// <summary>
        /// 创建文本比对设置页面，并监听语言切换以刷新导航标题。
        /// </summary>
        /// <param name="viewModel">文本比对设置页面 ViewModel。</param>
        public TcSettingsPage(TcSettingsViewModel viewModel)
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
        private void RefreshLocalizedTitle() => Title = Translations.Current["Page_CompareSettings"];
    }
}
