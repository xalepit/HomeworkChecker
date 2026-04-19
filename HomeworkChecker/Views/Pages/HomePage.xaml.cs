using Wpf.Ui.Abstractions.Controls;
using HomeworkChecker.UI.ViewModels.Pages;

namespace HomeworkChecker.UI.Views.Pages
{
    public partial class HomePage : INavigableView<HomeViewModel>
    {
        public HomeViewModel ViewModel { get; }

        public HomePage(HomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }


    }
}
