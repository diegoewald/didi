using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Suppliers;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class SuppliersCatalogView : UserControl
{
    private SupplierService? _supplierService;
    private SupplierListItemViewModel? _renameTarget;
    private bool _isCreating;
    private bool _isMutating;

    public SuppliersCatalogView()
    {
        InitializeComponent();
    }

    public void Configure(SupplierService supplierService)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating || _isMutating)
        {
            return;
        }

        CloseRenamePanel();
        HideMutationMessages();
        if (DataContext is SuppliersCatalogViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private async void CreateSupplier_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateSupplierAsync();
    }

    private async void CreateNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateSupplierAsync();
        }
    }


    private void OpenRename_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isMutating ||
            sender is not Button button ||
            button.DataContext is not SupplierListItemViewModel supplier)
        {
            return;
        }

        HideMutationMessages();
        _renameTarget = supplier;
        RenameTitle.Text = $"Renomear — {supplier.Name}";
        RenameTextBox.Text = supplier.Name;
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
            _supplierService is null ||
            sender is not Button button ||
            button.DataContext is not SupplierListItemViewModel supplier)
        {
            return;
        }

        var activate = !supplier.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                $"Desativar o fornecedor '{supplier.Name}'? Ele continuará no histórico e poderá ser reativado depois.",
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
            var updated = await _supplierService.SetActiveAsync(supplier.Id, activate);
            ApplySupplier(updated);
            ShowMutationSuccess(
                activate
                    ? $"Fornecedor '{updated.Name}' ativado com sucesso."
                    : $"Fornecedor '{updated.Name}' desativado com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para alterar fornecedores.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("O fornecedor não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change supplier active status from catalog screen");
            ShowMutationError("Não foi possível alterar o status do fornecedor. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isMutating = false;
            button.IsEnabled = true;
            button.Content = supplier.StatusAction;
        }
    }

    private async Task AttemptCreateSupplierAsync()
    {
        if (_isCreating || _isMutating || _supplierService is null)
        {
            return;
        }

        HideCreateMessages();
        SetCreateBusy(true);

        try
        {
            var created = await _supplierService.CreateAsync(CreateNameTextBox.Text);
            ApplySupplier(created);
            CreateNameTextBox.Clear();
            ShowCreateSuccess($"Fornecedor '{created.Name}' criado com sucesso.");
            CreateNameTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowCreateError("Você não tem permissão para criar fornecedores.");
        }
        catch (ArgumentException)
        {
            ShowCreateError("Informe um nome de fornecedor válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create supplier from catalog screen");
            ShowCreateError("Não foi possível criar o fornecedor. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetCreateBusy(false);
        }
    }

    private async Task AttemptRenameAsync()
    {
        if (_isCreating || _isMutating || _supplierService is null || _renameTarget is null)
        {
            return;
        }

        HideMutationMessages();
        var target = _renameTarget;
        SetRenameBusy(true);

        try
        {
            var updated = await _supplierService.RenameAsync(target.Id, RenameTextBox.Text);
            ApplySupplier(updated);
            CloseRenamePanel();
            ShowMutationSuccess($"Fornecedor renomeado para '{updated.Name}' com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para renomear fornecedores.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("O fornecedor não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowMutationError("Informe um nome de fornecedor válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to rename supplier from catalog screen");
            ShowMutationError("Não foi possível renomear o fornecedor. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetRenameBusy(false);
        }
    }

    private void ApplySupplier(SupplierResult supplier)
    {
        if (DataContext is SuppliersCatalogViewModel viewModel)
        {
            viewModel.ApplySupplier(supplier);
        }
    }

    private void SetCreateBusy(bool isBusy)
    {
        _isCreating = isBusy;
        CreateNameTextBox.IsEnabled = !isBusy;
        CreateSupplierButton.IsEnabled = !isBusy;
        CreateSupplierButton.Content = isBusy ? "Criando..." : "Criar fornecedor";
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
