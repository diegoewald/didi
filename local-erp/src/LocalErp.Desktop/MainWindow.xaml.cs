using System.Windows;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

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

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _authenticationService.SignOutAsync();
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Security audit write failed during sign-out");
        }
        finally
        {
            LogoutRequested = true;
            Close();
        }
    }
}
