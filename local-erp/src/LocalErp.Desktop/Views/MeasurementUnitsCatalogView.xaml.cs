using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Security;
using LocalErp.Application.Units;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class MeasurementUnitsCatalogView : UserControl
{
    private MeasurementUnitService? _measurementUnitService;
    private MeasurementUnitListItemViewModel? _renameTarget;
    private bool _isCreating;
    private bool _isMutating;

    public MeasurementUnitsCatalogView()
    {
        InitializeComponent();
    }

    public void Configure(MeasurementUnitService measurementUnitService)
    {
        _measurementUnitService = measurementUnitService ?? throw new ArgumentNullException(nameof(measurementUnitService));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating || _isMutating)
        {
            return;
        }

        CloseRenamePanel();
        HideMutationMessages();
        if (DataContext is MeasurementUnitsCatalogViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private async void CreateUnit_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateUnitAsync();
    }

    private async void CreateNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateUnitAsync();
        }
    }

    private void OpenRename_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isMutating ||
            sender is not Button button ||
            button.DataContext is not MeasurementUnitListItemViewModel unit)
        {
            return;
        }

        HideMutationMessages();
        _renameTarget = unit;
        RenameTitle.Text = $"Renomear — {unit.Name}";
        RenameTextBox.Text = unit.Name;
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
            _measurementUnitService is null ||
            sender is not Button button ||
            button.DataContext is not MeasurementUnitListItemViewModel unit)
        {
            return;
        }

        var activate = !unit.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                $"Desativar a unidade de medida '{unit.Name}'? Ela continuará no histórico e poderá ser reativada depois.",
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
            var updated = await _measurementUnitService.SetActiveAsync(unit.Id, activate);
            ApplyUnit(updated);
            ShowMutationSuccess(
                activate
                    ? $"Unidade de medida '{updated.Name}' ativada com sucesso."
                    : $"Unidade de medida '{updated.Name}' desativada com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para alterar unidades de medida.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("A unidade de medida não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change measurement unit active status from catalog screen");
            ShowMutationError("Não foi possível alterar o status da unidade de medida. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isMutating = false;
            button.IsEnabled = true;
            button.Content = unit.StatusAction;
        }
    }

    private async Task AttemptCreateUnitAsync()
    {
        if (_isCreating || _isMutating || _measurementUnitService is null)
        {
            return;
        }

        HideCreateMessages();
        SetCreateBusy(true);

        try
        {
            var created = await _measurementUnitService.CreateAsync(CreateNameTextBox.Text);
            ApplyUnit(created);
            CreateNameTextBox.Clear();
            ShowCreateSuccess($"Unidade de medida '{created.Name}' criada com sucesso.");
            CreateNameTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowCreateError("Você não tem permissão para criar unidades de medida.");
        }
        catch (ArgumentException)
        {
            ShowCreateError("Informe um nome de unidade de medida válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create measurement unit from catalog screen");
            ShowCreateError("Não foi possível criar a unidade de medida. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetCreateBusy(false);
        }
    }

    private async Task AttemptRenameAsync()
    {
        if (_isCreating || _isMutating || _measurementUnitService is null || _renameTarget is null)
        {
            return;
        }

        HideMutationMessages();
        var target = _renameTarget;
        SetRenameBusy(true);

        try
        {
            var updated = await _measurementUnitService.RenameAsync(target.Id, RenameTextBox.Text);
            ApplyUnit(updated);
            CloseRenamePanel();
            ShowMutationSuccess($"Unidade de medida renomeada para '{updated.Name}' com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para renomear unidades de medida.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("A unidade de medida não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowMutationError("Informe um nome de unidade de medida válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to rename measurement unit from catalog screen");
            ShowMutationError("Não foi possível renomear a unidade de medida. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetRenameBusy(false);
        }
    }

    private void ApplyUnit(MeasurementUnitResult unit)
    {
        if (DataContext is MeasurementUnitsCatalogViewModel viewModel)
        {
            viewModel.ApplyUnit(unit);
        }
    }

    private void SetCreateBusy(bool isBusy)
    {
        _isCreating = isBusy;
        CreateNameTextBox.IsEnabled = !isBusy;
        CreateUnitButton.IsEnabled = !isBusy;
        CreateUnitButton.Content = isBusy ? "Criando..." : "Criar unidade";
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
