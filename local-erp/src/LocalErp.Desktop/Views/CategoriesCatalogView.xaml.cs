using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Categories;
using LocalErp.Application.Security;
using LocalErp.Desktop.ViewModels;
using Serilog;

namespace LocalErp.Desktop.Views;

public partial class CategoriesCatalogView : UserControl
{
    private CategoryService? _categoryService;
    private CategoryListItemViewModel? _renameTarget;
    private bool _isCreating;
    private bool _isMutating;

    public CategoriesCatalogView()
    {
        InitializeComponent();
    }

    public void Configure(CategoryService categoryService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating || _isMutating)
        {
            return;
        }

        CloseRenamePanel();
        HideMutationMessages();
        if (DataContext is CategoriesCatalogViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    private async void CreateCategory_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateCategoryAsync();
    }

    private async void CreateNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateCategoryAsync();
        }
    }

    private void OpenRename_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isMutating ||
            sender is not Button button ||
            button.DataContext is not CategoryListItemViewModel category)
        {
            return;
        }

        HideMutationMessages();
        _renameTarget = category;
        RenameTitle.Text = $"Renomear — {category.Name}";
        RenameTextBox.Text = category.Name;
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
            _categoryService is null ||
            sender is not Button button ||
            button.DataContext is not CategoryListItemViewModel category)
        {
            return;
        }

        var activate = !category.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                $"Desativar a categoria '{category.Name}'? Ela continuará no histórico e poderá ser reativada depois.",
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
            var updated = await _categoryService.SetActiveAsync(category.Id, activate);
            ApplyCategory(updated);
            ShowMutationSuccess(
                activate
                    ? $"Categoria '{updated.Name}' ativada com sucesso."
                    : $"Categoria '{updated.Name}' desativada com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para alterar categorias.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("A categoria não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change category active status from catalog screen");
            ShowMutationError("Não foi possível alterar o status da categoria. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isMutating = false;
            button.IsEnabled = true;
            button.Content = category.StatusAction;
        }
    }

    private async Task AttemptCreateCategoryAsync()
    {
        if (_isCreating || _isMutating || _categoryService is null)
        {
            return;
        }

        HideCreateMessages();
        SetCreateBusy(true);

        try
        {
            var created = await _categoryService.CreateAsync(CreateNameTextBox.Text);
            ApplyCategory(created);
            CreateNameTextBox.Clear();
            ShowCreateSuccess($"Categoria '{created.Name}' criada com sucesso.");
            CreateNameTextBox.Focus();
        }
        catch (AccessDeniedException)
        {
            ShowCreateError("Você não tem permissão para criar categorias.");
        }
        catch (ArgumentException)
        {
            ShowCreateError("Informe um nome de categoria válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create category from catalog screen");
            ShowCreateError("Não foi possível criar a categoria. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetCreateBusy(false);
        }
    }

    private async Task AttemptRenameAsync()
    {
        if (_isCreating || _isMutating || _categoryService is null || _renameTarget is null)
        {
            return;
        }

        HideMutationMessages();
        var target = _renameTarget;
        SetRenameBusy(true);

        try
        {
            var updated = await _categoryService.RenameAsync(target.Id, RenameTextBox.Text);
            ApplyCategory(updated);
            CloseRenamePanel();
            ShowMutationSuccess($"Categoria renomeada para '{updated.Name}' com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowMutationError("Você não tem permissão para renomear categorias.");
        }
        catch (KeyNotFoundException)
        {
            ShowMutationError("A categoria não foi encontrada. Atualize a lista e tente novamente.");
        }
        catch (ArgumentException)
        {
            ShowMutationError("Informe um nome de categoria válido com até 200 caracteres.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to rename category from catalog screen");
            ShowMutationError("Não foi possível renomear a categoria. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetRenameBusy(false);
        }
    }

    private void ApplyCategory(CategoryResult category)
    {
        if (DataContext is CategoriesCatalogViewModel viewModel)
        {
            viewModel.ApplyCategory(category);
        }
    }

    private void SetCreateBusy(bool isBusy)
    {
        _isCreating = isBusy;
        CreateNameTextBox.IsEnabled = !isBusy;
        CreateCategoryButton.IsEnabled = !isBusy;
        CreateCategoryButton.Content = isBusy ? "Criando..." : "Criar categoria";
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
