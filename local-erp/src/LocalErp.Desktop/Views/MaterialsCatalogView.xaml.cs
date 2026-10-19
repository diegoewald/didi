using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Materials;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class MaterialsCatalogView : UserControl
{
    private MaterialService? _materialService;
    private MaterialListItemViewModel? _renameTarget;
    private bool _isCreating;
    private bool _isMutating;

    public MaterialsCatalogView()
    {
        InitializeComponent();
    }

    public void Configure(MaterialService materialService)
    {
        _materialService = materialService ?? throw new ArgumentNullException(nameof(materialService));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating || _isMutating)
        {
            return;
        }

        CloseRenamePanel();
        HideMutationMessages();
        if (DataContext is MaterialsCatalogViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private async void CreateMaterial_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateMaterialAsync();
    }

    private async void CreateNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateMaterialAsync();
        }
    }

    private void OpenRename_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isMutating ||
            sender is not Button button ||
            button.DataContext is not MaterialListItemViewModel material)
        {
            return;
        }

        HideMutationMessages();
        _renameTarget = material;
        RenameTitle.Text = $"Renomear — {material.Name}";
        RenameTextBox.Text = material.Name;
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
            _materialService is null ||
            sender is not Button button ||
            button.DataContext is not MaterialListItemViewModel material)
        {
            return;
        }

        var activate = !material.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                $"Desativar o material '{material.Name}'? Ele continuará no histórico e poderá ser reativado depois.",
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
            var updated = await _materialService.SetActiveAsync(material.Id, activate);
            ApplyMaterial(updated);
            ShowMutationSuccess(
                activate
                    ? $"Material '{updated.Name}' ativado com sucesso."
                    : $"Material '{updated.Name}' desativado com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para alterar materiais.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("O material não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change material active status from catalog screen");
            ShowMutationError("Não foi possível alterar o status do material. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isMutating = false;
            button.IsEnabled = true;
            button.Content = material.StatusAction;
        }
    }

    private async Task AttemptCreateMaterialAsync()
    {
        if (_isCreating || _isMutating || _materialService is null)
        {
            return;
        }

        HideCreateMessages();
        SetCreateBusy(true);

        try
        {
            var created = await _materialService.CreateAsync(CreateNameTextBox.Text);
            ApplyMaterial(created);
            CreateNameTextBox.Clear();
            ShowCreateSuccess($"Material '{created.Name}' criado com sucesso.");
            CreateNameTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowCreateError("Você não tem permissão para criar materiais.");
        }
        catch (ArgumentException)
        {
            ShowCreateError("Informe um nome de material válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create material from catalog screen");
            ShowCreateError("Não foi possível criar o material. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetCreateBusy(false);
        }
    }

    private async Task AttemptRenameAsync()
    {
        if (_isCreating || _isMutating || _materialService is null || _renameTarget is null)
        {
            return;
        }

        HideMutationMessages();
        var target = _renameTarget;
        SetRenameBusy(true);

        try
        {
            var updated = await _materialService.RenameAsync(target.Id, RenameTextBox.Text);
            ApplyMaterial(updated);
            CloseRenamePanel();
            ShowMutationSuccess($"Material renomeado para '{updated.Name}' com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para renomear materiais.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("O material não foi encontrado. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowMutationError("Informe um nome de material válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to rename material from catalog screen");
            ShowMutationError("Não foi possível renomear o material. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetRenameBusy(false);
        }
    }

    private void ApplyMaterial(MaterialResult material)
    {
        if (DataContext is MaterialsCatalogViewModel viewModel)
        {
            viewModel.ApplyMaterial(material);
        }
    }

    private void SetCreateBusy(bool isBusy)
    {
        _isCreating = isBusy;
        CreateNameTextBox.IsEnabled = !isBusy;
        CreateMaterialButton.IsEnabled = !isBusy;
        CreateMaterialButton.Content = isBusy ? "Criando..." : "Criar material";
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
