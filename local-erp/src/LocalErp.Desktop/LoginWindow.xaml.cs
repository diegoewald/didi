using System.Windows;
using System.Windows.Input;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop;

public partial class LoginWindow : Window
{
    private readonly AuthenticationService _authenticationService;

    public LoginWindow(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

        InitializeComponent();
        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    private async void SignIn_Click(object sender, RoutedEventArgs e)
    {
        await AttemptSignInAsync();
    }

    private async void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptSignInAsync();
        }
    }

    private async Task AttemptSignInAsync()
    {
        HideError();

        var username = UsernameTextBox.Text;
        var password = PasswordInput.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Informe usuário e senha.");
            return;
        }

        SetBusy(true);

        try
        {
            var result = await _authenticationService.SignInAsync(username, password);
            PasswordInput.Clear();

            if (!result.Succeeded)
            {
                ShowError("Usuário ou senha inválidos.");
                PasswordInput.Focus();
                return;
            }

            DialogResult = true;
        }
        catch (ArgumentException)
        {
            PasswordInput.Clear();
            ShowError("Usuário ou senha inválidos.");
        }
        catch (Exception exception)
        {
            PasswordInput.Clear();
            Log.Error(exception, "Unexpected error during sign-in");
            ShowError("Não foi possível entrar agora. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        PasswordInput.Clear();
        DialogResult = false;
    }

    private void SetBusy(bool isBusy)
    {
        UsernameTextBox.IsEnabled = !isBusy;
        PasswordInput.IsEnabled = !isBusy;
        SignInButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
        SignInButton.Content = isBusy ? "Entrando..." : "Entrar";
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;
    }
}
