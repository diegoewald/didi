using System.Windows;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;

namespace LocalErp.Desktop;

public partial class MainWindow : Window
{
    private readonly AuthenticationService _authenticationService;

    public MainWindow(
        MainWindowViewModel viewModel,
        AuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

        InitializeComponent();
        DataContext = viewModel;
    }

    public bool LogoutRequested { get; private set; }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _authenticationService.SignOut();
        LogoutRequested = true;
        Close();
    }
}
