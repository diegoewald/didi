using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Companies;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class CompaniesCatalogView : UserControl
{
    private CompanyService? _companyService;
    private CompanyListItemViewModel? _renameTarget;
    private bool _isCreating;
    private bool _isMutating;

    public CompaniesCatalogView()
    {
        InitializeComponent();
    }

    public void Configure(CompanyService companyService)
    {
        _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating || _isMutating)
        {
            return;
        }

        CloseRenamePanel();
        HideMutationMessages();
        if (DataContext is CompaniesCatalogViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private async void CreateCompany_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateCompanyAsync();
    }

    private async void CreateNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateCompanyAsync();
        }
    }

    private void OpenRename_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isMutating ||
            sender is not Button button ||
            button.DataContext is not CompanyListItemViewModel company)
        {
            return;
        }

        HideMutationMessages();
        _renameTarget = company;
        RenameTitle.Text = $"Renomear — {company.Name}";
        RenameTextBox.Text = company.Name;
        RenamePanel.Visibility = Visibility.Visible;
        RenameTextBox.Focus();
        RenameTextBox.SelectAll();
    }

    private async void RenameSave_Click(object sender, RoutedEventArgs e)
    {
        await AttemptRenameAsync();
    }

    private async void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptRenameAsync();
        }
        else if (e.Key == Key.Escape && !_isMutating)
        {
            e.Handled = true;
            CloseRenamePanel();
        }
    }

    private void RenameCancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isMutating)
        {
            CloseRenamePanel();
        }
    }

    private async void ToggleStatus_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isMutating ||
            _companyService is null ||
            sender is not Button button ||
            button.DataContext is not CompanyListItemViewModel company)
        {
            return;
        }

        var activate = !company.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                $"Desativar a empresa '{company.Name}'? Ela continuará no histórico e poderá ser reativada depois.",
                "Confirmar desativação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }

        CloseRenamePanel();
        HideMutationMessages();
        _isMutating = true;
        button.IsEnabled = false;
        button.Content = activate ? "Ativando..." : "Desativando...";

        try
        {
            var updated = await _companyService.SetActiveAsync(company.Id, activate);
            ApplyCompany(updated);
            ShowMutationSuccess(
                activate
                    ? $"Empresa '{updated.Name}' ativada com sucesso."
                    : $"Empresa '{updated.Name}' desativada com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para alterar empresas.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("A empresa não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change company active status from catalog screen");
            ShowMutationError("Não foi possível alterar o status da empresa. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isMutating = false;
            button.IsEnabled = true;
            button.Content = company.StatusAction;
        }
    }

    private async Task AttemptCreateCompanyAsync()
    {
        if (_isCreating || _isMutating || _companyService is null)
        {
            return;
        }

        HideCreateMessages();
        SetCreateBusy(true);

        try
        {
            var created = await _companyService.CreateAsync(CreateNameTextBox.Text);
            ApplyCompany(created);
            CreateNameTextBox.Clear();
            ShowCreateSuccess($"Empresa '{created.Name}' criada com sucesso.");
            CreateNameTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowCreateError("Você não tem permissão para criar empresas.");
        }
        catch (ArgumentException)
        {
            ShowCreateError("Informe um nome de empresa válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create company from catalog screen");
            ShowCreateError("Não foi possível criar a empresa. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetCreateBusy(false);
        }
    }

    private async Task AttemptRenameAsync()
    {
        if (_isCreating || _isMutating || _companyService is null || _renameTarget is null)
        {
            return;
        }

        HideMutationMessages();
        var target = _renameTarget;
        SetRenameBusy(true);

        try
        {
            var updated = await _companyService.RenameAsync(target.Id, RenameTextBox.Text);
            ApplyCompany(updated);
            CloseRenamePanel();
            ShowMutationSuccess($"Empresa renomeada para '{updated.Name}' com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para renomear empresas.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("A empresa não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowMutationError("Informe um nome de empresa válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to rename company from catalog screen");
            ShowMutationError("Não foi possível renomear a empresa. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetRenameBusy(false);
        }
    }

    private void ApplyCompany(CompanyResult company)
    {
        if (DataContext is CompaniesCatalogViewModel viewModel)
        {
            viewModel.ApplyCompany(company);
        }
    }

    private void SetCreateBusy(bool isBusy)
    {
        _isCreating = isBusy;
        CreateNameTextBox.IsEnabled = !isBusy;
        CreateCompanyButton.IsEnabled = !isBusy;
        CreateCompanyButton.Content = isBusy ? "Criando..." : "Criar empresa";
    }

    private void SetRenameBusy(bool isBusy)
    {
        _isMutating = isBusy;
        RenameTextBox.IsEnabled = !isBusy;
        RenameSaveButton.IsEnabled = !isBusy;
        RenameCancelButton.IsEnabled = !isBusy;
        RenameSaveButton.Content = isBusy ? "Salvando..." : "Salvar";
    }

    private void CloseRenamePanel()
    {
        _renameTarget = null;
        RenameTextBox.Clear();
        RenameTitle.Text = string.Empty;
        RenamePanel.Visibility = Visibility.Collapsed;
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

    private void HideMutationMessages()
    {
        MutationErrorPanel.Visibility = Visibility.Collapsed;
        MutationSuccessPanel.Visibility = Visibility.Collapsed;
        MutationErrorText.Text = string.Empty;
        MutationSuccessText.Text = string.Empty;
    }

    private void ShowMutationError(string message)
    {
        MutationSuccessPanel.Visibility = Visibility.Collapsed;
        MutationErrorText.Text = message;
        MutationErrorPanel.Visibility = Visibility.Visible;
    }

    private void ShowMutationSuccess(string message)
    {
        MutationErrorPanel.Visibility = Visibility.Collapsed;
        MutationSuccessText.Text = message;
        MutationSuccessPanel.Visibility = Visibility.Visible;
    }
}
