using HomeworkChecker.UI.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace HomeworkChecker.UI.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        private readonly IContentDialogService _contentDialogService;

        [RelayCommand]
        private async Task SelectAccentColorButton()
        {
            var dialog = new ContentDialog
            {
                Title = "选择颜色",
                Content = new TextBlock
                {
                    Text = "调色盘暂未实现！日后上线！"
                },
                PrimaryButtonText = "确认",
                CloseButtonText = "取消"
            };

            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
        }
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel, IContentDialogService contentDialogService)
        {
            ViewModel = viewModel;
            _contentDialogService = contentDialogService;
            DataContext = this;

            InitializeComponent();
        }
    }
}
