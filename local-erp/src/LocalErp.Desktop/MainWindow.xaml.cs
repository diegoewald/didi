using System.Windows;
using LocalErp.Desktop.ViewModels;

namespace LocalErp.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
