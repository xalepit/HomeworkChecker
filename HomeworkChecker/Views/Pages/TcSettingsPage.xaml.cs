using HomeworkChecker.UI.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Abstractions.Controls;

namespace HomeworkChecker.UI.Views.Pages
{
    public partial class TcSettingsPage : INavigableView<TcSettingsViewModel>
    {
        public TcSettingsViewModel ViewModel { get; }

        public TcSettingsPage(TcSettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

    }
}