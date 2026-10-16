using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class AdministrationUsersView : UserControl
{
    private UserService? _userService;
    private bool _isCreating;

    public AdministrationUsersView()
    {
        InitializeComponent();
        PasswordPolicyText.Text =
            $"Use entre {PasswordPolicy.MinimumLength} e {PasswordPolicy.MaximumLength} caracteres.";
    }

    public void Configure(UserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateUserAsync();
    }

    private async void ConfirmPasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateUserAsync();
        }
    }

    private async Task AttemptCreateUserAsync()
    {
        if (_isCreating || _userService is null)
        {
            return;
        }

        HideCreateMessages();

        var password = CreatePasswordInput.Password;
        var passwordConfirmation = ConfirmPasswordInput.Password;
        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
        {
            ShowCreateError("As senhas informadas não coincidem.");
            CreatePasswordInput.Clear();
            ConfirmPasswordInput.Clear();
            CreatePasswordInput.Focus();
            return;
        }

        SetCreateBusy(true);

        try
        {
            var created = await _userService.CreateAsync(CreateUsernameTextBox.Text, password);

            if (DataContext is UsersAdministrationViewModel viewModel)
            {
                viewModel.ApplyCreatedUser(created);
            }

            CreateUsernameTextBox.Clear();
            ShowCreateSuccess($"Usuário '{created.Username}' criado com sucesso.");
            CreateUsernameTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowCreateError("Você não tem permissão para criar usuários.");
        }
        catch (InvalidOperationException)
        {
            ShowCreateError("Já existe um usuário com esse nome.");
        }
        catch (ArgumentException)
        {
            ShowCreateError("Verifique o nome de usuário e a política da senha.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create user from administration screen");
            ShowCreateError("Não foi possível criar o usuário. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            CreatePasswordInput.Clear();
            ConfirmPasswordInput.Clear();
            SetCreateBusy(false);
        }
    }

    private void SetCreateBusy(bool isBusy)
    {
        _isCreating = isBusy;
        CreateUsernameTextBox.IsEnabled = !isBusy;
        CreatePasswordInput.IsEnabled = !isBusy;
        ConfirmPasswordInput.IsEnabled = !isBusy;
        CreateUserButton.IsEnabled = !isBusy;
        CreateUserButton.Content = isBusy ? "Criando..." : "Criar usuário";
    }

    private void HideCreateMessages()
    {
        CreateErrorPanel.Visibility = Visibility.Collapsed;
        CreateSuccessPanel.Visibility = Visibility.Collapsed;
        CreateErrorText.Text = string.Empty;
        CreateSuccessText.Text = string.Empty;
    }

    private void ShowCreateError(string message)
    {
        CreateSuccessPanel.Visibility = Visibility.Collapsed;
        CreateErrorText.Text = message;
        CreateErrorPanel.Visibility = Visibility.Visible;
    }

    private void ShowCreateSuccess(string message)
    {
        CreateErrorPanel.Visibility = Visibility.Collapsed;
        CreateSuccessText.Text = message;
        CreateSuccessPanel.Visibility = Visibility.Visible;
    }
}
