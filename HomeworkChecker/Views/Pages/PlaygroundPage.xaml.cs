using HomeworkChecker.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.Views.Pages
{
    public partial class PlaygroundPage : INavigableView<PlaygroundViewModel>
    {
        public PlaygroundViewModel ViewModel { get; }

        public PlaygroundPage(PlaygroundViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
