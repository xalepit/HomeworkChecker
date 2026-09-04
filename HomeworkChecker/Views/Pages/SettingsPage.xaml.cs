using HomeworkChecker.UI.ViewModels.Pages;
using HomeworkChecker.UI.Resources;
using HomeworkChecker.UI.ViewModels.Dialogs;
using HomeworkChecker.UI.Views.Dialogs;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace HomeworkChecker.UI.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        private readonly IContentDialogService _contentDialogService;

        /// <summary>
        /// 显示主题色调色盘，并在用户确认后保存和应用颜色。
        /// </summary>
        [RelayCommand]
        private async Task SelectAccentColorButton()
        {
            var colorPickerViewModel = new AccentColorPickerViewModel(
                ViewModel.PersonalizationSettings.CustomAccentColor);
            var dialog = new ContentDialog
            {
                Title = Translations.ColorPicker_Title,
                Content = new AccentColorPickerView(colorPickerViewModel),
                MinWidth = 760,
                PrimaryButtonText = Translations.Common_Confirm,
                CloseButtonText = Translations.Common_Cancel
            };

            var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.PersonalizationSettings.ApplyCustomAccentColor(colorPickerViewModel.HexColor);
            }
        }

        public SettingsViewModel ViewModel { get; }

        /// <summary>
        /// 创建通用设置页面并连接页面级对话框服务。
        /// </summary>
        /// <param name="viewModel">通用设置页面 ViewModel。</param>
        /// <param name="contentDialogService">内容对话框服务。</param>
        public SettingsPage(SettingsViewModel viewModel, IContentDialogService contentDialogService)
        {
            ViewModel = viewModel;
            _contentDialogService = contentDialogService;
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
        private void RefreshLocalizedTitle() => Title = Translations.Current["Page_Settings"];

    }
}
