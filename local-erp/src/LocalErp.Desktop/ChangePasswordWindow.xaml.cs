using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LocalErp.Application.Account;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop;

public partial class ChangePasswordWindow : Window
{
    private readonly AccountService _accountService;
    private bool _isSaving;

    public ChangePasswordWindow(AccountService accountService)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));

        InitializeComponent();
        PasswordPolicyText.Text =
            $"Use entre {PasswordPolicy.MinimumLength} e {PasswordPolicy.MaximumLength} caracteres.";
        CurrentPasswordInput.Focus();
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        await AttemptChangePasswordAsync();
    }

    private async void ConfirmNewPasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptChangePasswordAsync();
        }
    }

    private async Task AttemptChangePasswordAsync()
    {
        if (_isSaving)
        {
            return;
        }

        HideError();

        var currentPassword = CurrentPasswordInput.Password;
        var newPassword = NewPasswordInput.Password;
        var confirmation = ConfirmNewPasswordInput.Password;

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            ShowError("Informe a senha atual.");
            ClearSensitiveFields();
            CurrentPasswordInput.Focus();
            return;
        }

        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
        {
            ShowError("A nova senha e a confirmação não coincidem.");
            ClearSensitiveFields();
            CurrentPasswordInput.Focus();
            return;
        }

        SetBusy(true);
        var succeeded = false;

        try
        {
            await _accountService.ChangePasswordAsync(currentPassword, newPassword);
            succeeded = true;
        }
        catch (CurrentPasswordInvalidException)
        {
            ShowError("A senha atual está incorreta.");
        }
        catch (ArgumentException)
        {
            ShowError("A nova senha não atende à política exigida.");
        }
        catch (InvalidOperationException)
        {
            ShowError(
                "A conta ou a credencial mudou enquanto esta janela estava aberta. Saia do ERP, entre novamente e repita a operação.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change own account password");
            ShowError("Não foi possível alterar a senha. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            ClearSensitiveFields();
            SetBusy(false);
        }

        if (succeeded)
        {
            MessageBox.Show(
                this,
                "Senha alterada com sucesso.",
                "Senha atualizada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            DialogResult = true;
        }
        else
        {
            CurrentPasswordInput.Focus();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isSaving)
        {
            ClearSensitiveFields();
            Close();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isSaving)
        {
            e.Cancel = true;
            return;
        }

        ClearSensitiveFields();
        base.OnClosing(e);
    }

    private void SetBusy(bool isBusy)
    {
        _isSaving = isBusy;
        CurrentPasswordInput.IsEnabled = !isBusy;
        NewPasswordInput.IsEnabled = !isBusy;
        ConfirmNewPasswordInput.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
        ChangePasswordButton.IsEnabled = !isBusy;
        ChangePasswordButton.Content = isBusy ? "Alterando..." : "Alterar senha";
    }

    private void ClearSensitiveFields()
    {
        CurrentPasswordInput.Clear();
        NewPasswordInput.Clear();
        ConfirmNewPasswordInput.Clear();
    }

    private void HideError()
    {
        ErrorText.Text = string.Empty;
        ErrorPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }
}
