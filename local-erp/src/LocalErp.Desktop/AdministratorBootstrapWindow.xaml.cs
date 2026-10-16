using System.Windows;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop;

public partial class AdministratorBootstrapWindow : Window
{
    private readonly AdministratorBootstrapService _bootstrapService;
    private readonly AuthenticationService _authenticationService;

    public AdministratorBootstrapWindow(
        AdministratorBootstrapService bootstrapService,
        AuthenticationService authenticationService)
    {
        _bootstrapService = bootstrapService ?? throw new ArgumentNullException(nameof(bootstrapService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

        InitializeComponent();
        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    private async void CreateAdministrator_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        var username = UsernameTextBox.Text;
        var password = PasswordInput.Password;
        var confirmation = ConfirmPasswordInput.Password;

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            ShowError("As senhas informadas não coincidem.");
            ConfirmPasswordInput.Clear();
            ConfirmPasswordInput.Focus();
            return;
        }

        SetBusy(true);

        try
        {
            await _bootstrapService.BootstrapAsync(username, password);

            var signIn = await _authenticationService.SignInAsync(username, password);
            PasswordInput.Clear();
            ConfirmPasswordInput.Clear();

            if (!signIn.Succeeded)
            {
                ShowError("A conta foi criada, mas não foi possível iniciar a sessão. Abra o aplicativo novamente e entre com a conta criada.");
                return;
            }

            DialogResult = true;
        }
        catch (ArgumentException)
        {
            ShowError($"Informe um usuário válido e uma senha entre {PasswordPolicy.MinimumLength} e {PasswordPolicy.MaximumLength} caracteres.");
        }
        catch (InvalidOperationException exception)
        {
            Log.Warning(exception, "Administrator bootstrap was already completed");
            PasswordInput.Clear();
            ConfirmPasswordInput.Clear();
            DialogResult = true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Unexpected error while creating the first administrator");
            ShowError("Não foi possível concluir a configuração inicial. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        PasswordInput.Clear();
        ConfirmPasswordInput.Clear();
        DialogResult = false;
    }

    private void SetBusy(bool isBusy)
    {
        UsernameTextBox.IsEnabled = !isBusy;
        PasswordInput.IsEnabled = !isBusy;
        ConfirmPasswordInput.IsEnabled = !isBusy;
        CreateButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
        CreateButton.Content = isBusy ? "Criando..." : "Criar administrador";
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
