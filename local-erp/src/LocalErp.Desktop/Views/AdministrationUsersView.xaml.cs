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
    private UserListItemViewModel? _passwordResetTarget;
    private bool _isCreating;
    private bool _isChangingStatus;
    private bool _isResettingPassword;

    public AdministrationUsersView()
    {
        InitializeComponent();
        PasswordPolicyText.Text =
            $"Use entre {PasswordPolicy.MinimumLength} e {PasswordPolicy.MaximumLength} caracteres.";
        ResetPasswordPolicyText.Text = PasswordPolicyText.Text;
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

    private async void ToggleUserStatus_Click(object sender, RoutedEventArgs e)
    {
        if (_isChangingStatus ||
            _isResettingPassword ||
            _userService is null ||
            sender is not Button button ||
            button.DataContext is not UserListItemViewModel user)
        {
            return;
        }

        var activate = !user.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                $"Desativar o usuário '{user.Username}'? Ele não poderá entrar no ERP até ser reativado.",
                "Confirmar desativação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }

        ClosePasswordResetPanel();
        HideStatusMessages();
        _isChangingStatus = true;
        button.IsEnabled = false;
        button.Content = activate ? "Ativando..." : "Desativando...";

        try
        {
            var updated = await _userService.SetActiveAsync(user.Id, activate);

            if (DataContext is UsersAdministrationViewModel viewModel)
            {
                viewModel.ApplyUser(updated);
            }

            ShowStatusSuccess(
                activate
                    ? $"Usuário '{updated.Username}' ativado com sucesso."
                    : $"Usuário '{updated.Username}' desativado com sucesso.");
        }
        catch (SelfDeactivationNotAllowedException)
        {
            ShowStatusError("A conta usada nesta sessão não pode ser desativada por ela mesma.");
        }
        catch (AccessDeniedException)
        {
            ShowStatusError("Você não tem permissão para alterar o status de usuários.");
        }
        catch (KeyNotFoundException)
        {
            ShowStatusError("O usuário não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change user active status from administration screen");
            ShowStatusError("Não foi possível alterar o status do usuário. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isChangingStatus = false;
            button.IsEnabled = true;
            button.Content = user.StatusAction;
        }
    }

    private void OpenPasswordReset_Click(object sender, RoutedEventArgs e)
    {
        if (_isChangingStatus ||
            _isResettingPassword ||
            sender is not Button button ||
            button.DataContext is not UserListItemViewModel user)
        {
            return;
        }

        HideStatusMessages();
        _passwordResetTarget = user;
        PasswordResetTitle.Text = $"Redefinir senha — {user.Username}";
        ResetPasswordInput.Clear();
        ResetPasswordConfirmationInput.Clear();
        HideResetPasswordError();
        PasswordResetPanel.Visibility = Visibility.Visible;
        ResetPasswordInput.Focus();
    }

    private async void ResetPasswordConfirm_Click(object sender, RoutedEventArgs e)
    {
        await AttemptResetPasswordAsync();
    }

    private async void ResetPasswordConfirmationInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptResetPasswordAsync();
        }
    }

    private void ResetPasswordCancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isResettingPassword)
        {
            ClosePasswordResetPanel();
        }
    }

    private async Task AttemptResetPasswordAsync()
    {
        if (_isResettingPassword || _userService is null || _passwordResetTarget is null)
        {
            return;
        }

        HideResetPasswordError();

        var password = ResetPasswordInput.Password;
        var passwordConfirmation = ResetPasswordConfirmationInput.Password;
        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
        {
            ShowResetPasswordError("As senhas informadas não coincidem.");
            ResetPasswordInput.Clear();
            ResetPasswordConfirmationInput.Clear();
            ResetPasswordInput.Focus();
            return;
        }

        var target = _passwordResetTarget;
        SetResetPasswordBusy(true);

        try
        {
            var updated = await _userService.ChangePasswordAsync(target.Id, password);
            ClosePasswordResetPanel();
            ShowStatusSuccess(
                $"Senha do usuário '{updated.Username}' redefinida com sucesso. Tentativas falhas e bloqueio temporário foram removidos.");
        }
        catch (AccessDeniedException)
        {
            ShowResetPasswordError("Você não tem permissão para redefinir senhas de usuários.");
        }
        catch (KeyNotFoundException)
        {
            ShowResetPasswordError("O usuário não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowResetPasswordError("A nova senha não atende à política exigida.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to reset user password from administration screen");
            ShowResetPasswordError("Não foi possível redefinir a senha. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            ResetPasswordInput.Clear();
            ResetPasswordConfirmationInput.Clear();
            SetResetPasswordBusy(false);
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

    private void SetResetPasswordBusy(bool isBusy)
    {
        _isResettingPassword = isBusy;
        ResetPasswordInput.IsEnabled = !isBusy;
        ResetPasswordConfirmationInput.IsEnabled = !isBusy;
        ResetPasswordConfirmButton.IsEnabled = !isBusy;
        ResetPasswordCancelButton.IsEnabled = !isBusy;
        ResetPasswordConfirmButton.Content = isBusy ? "Redefinindo..." : "Redefinir senha";
    }

    private void ClosePasswordResetPanel()
    {
        _passwordResetTarget = null;
        ResetPasswordInput.Clear();
        ResetPasswordConfirmationInput.Clear();
        HideResetPasswordError();
        PasswordResetPanel.Visibility = Visibility.Collapsed;
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

    private void HideStatusMessages()
    {
        StatusErrorPanel.Visibility = Visibility.Collapsed;
        StatusSuccessPanel.Visibility = Visibility.Collapsed;
        StatusErrorText.Text = string.Empty;
        StatusSuccessText.Text = string.Empty;
    }

    private void ShowStatusError(string message)
    {
        StatusSuccessPanel.Visibility = Visibility.Collapsed;
        StatusErrorText.Text = message;
        StatusErrorPanel.Visibility = Visibility.Visible;
    }

    private void ShowStatusSuccess(string message)
    {
        StatusErrorPanel.Visibility = Visibility.Collapsed;
        StatusSuccessText.Text = message;
        StatusSuccessPanel.Visibility = Visibility.Visible;
    }

    private void HideResetPasswordError()
    {
        ResetPasswordErrorPanel.Visibility = Visibility.Collapsed;
        ResetPasswordErrorText.Text = string.Empty;
    }

    private void ShowResetPasswordError(string message)
    {
        ResetPasswordErrorText.Text = message;
        ResetPasswordErrorPanel.Visibility = Visibility.Visible;
    }
}
