using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.CostCenters;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class CostCentersCatalogView : UserControl
{
    private CostCenterService? _costCenterService;
    private CostCenterListItemViewModel? _renameTarget;
    private bool _isCreating;
    private bool _isMutating;

    public CostCentersCatalogView()
    {
        InitializeComponent();
    }

    public void Configure(CostCenterService costCenterService)
    {
        _costCenterService = costCenterService ?? throw new ArgumentNullException(nameof(costCenterService));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating || _isMutating)
        {
            return;
        }

        CloseRenamePanel();
        HideMutationMessages();
        if (DataContext is CostCentersCatalogViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private async void CreateCostCenter_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateCostCenterAsync();
    }

    private async void CreateNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateCostCenterAsync();
        }
    }

    private void OpenRename_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isMutating ||
            sender is not Button button ||
            button.DataContext is not CostCenterListItemViewModel costCenter)
        {
            return;
        }

        HideMutationMessages();
        _renameTarget = costCenter;
        RenameTitle.Text = $"Renomear — {costCenter.Name}";
        RenameTextBox.Text = costCenter.Name;
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
            _costCenterService is null ||
            sender is not Button button ||
            button.DataContext is not CostCenterListItemViewModel costCenter)
        {
            return;
        }

        var activate = !costCenter.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                $"Desativar o centro de custo '{costCenter.Name}'? Ele continuará no histórico e poderá ser reativado depois.",
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
            var updated = await _costCenterService.SetActiveAsync(costCenter.Id, activate);
            ApplyCostCenter(updated);
            ShowMutationSuccess(
                activate
                    ? $"Centro de custo '{updated.Name}' ativado com sucesso."
                    : $"Centro de custo '{updated.Name}' desativado com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para alterar centros de custo.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("O centro de custo não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change cost center active status from catalog screen");
            ShowMutationError("Não foi possível alterar o status do centro de custo. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isMutating = false;
            button.IsEnabled = true;
            button.Content = costCenter.StatusAction;
        }
    }

    private async Task AttemptCreateCostCenterAsync()
    {
        if (_isCreating || _isMutating || _costCenterService is null)
        {
            return;
        }

        HideCreateMessages();
        SetCreateBusy(true);

        try
        {
            var created = await _costCenterService.CreateAsync(CreateNameTextBox.Text);
            ApplyCostCenter(created);
            CreateNameTextBox.Clear();
            ShowCreateSuccess($"Centro de custo '{created.Name}' criado com sucesso.");
            CreateNameTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowCreateError("Você não tem permissão para criar centros de custo.");
        }
        catch (ArgumentException)
        {
            ShowCreateError("Informe um nome de centro de custo válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create cost center from catalog screen");
            ShowCreateError("Não foi possível criar o centro de custo. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetCreateBusy(false);
        }
    }

    private async Task AttemptRenameAsync()
    {
        if (_isCreating || _isMutating || _costCenterService is null || _renameTarget is null)
        {
            return;
        }

        HideMutationMessages();
        var target = _renameTarget;
        SetRenameBusy(true);

        try
        {
            var updated = await _costCenterService.RenameAsync(target.Id, RenameTextBox.Text);
            ApplyCostCenter(updated);
            CloseRenamePanel();
            ShowMutationSuccess($"Centro de custo renomeado para '{updated.Name}' com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para renomear centros de custo.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("O centro de custo não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowMutationError("Informe um nome de centro de custo válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to rename cost center from catalog screen");
            ShowMutationError("Não foi possível renomear o centro de custo. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetRenameBusy(false);
        }
    }

    private void ApplyCostCenter(CostCenterResult costCenter)
    {
        if (DataContext is CostCentersCatalogViewModel viewModel)
        {
            viewModel.ApplyCostCenter(costCenter);
        }
    }

    private void SetCreateBusy(bool isBusy)
    {
        _isCreating = isBusy;
        CreateNameTextBox.IsEnabled = !isBusy;
        CreateCostCenterButton.IsEnabled = !isBusy;
        CreateCostCenterButton.Content = isBusy ? "Criando..." : "Criar centro de custo";
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
