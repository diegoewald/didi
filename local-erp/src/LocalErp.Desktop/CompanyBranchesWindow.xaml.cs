using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Companies;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop;

public partial class CompanyBranchesWindow : Window
{
    private readonly CompanyBranchService _branchService;
    private readonly Guid _companyId;
    private readonly string _companyName;
    private List<CompanyBranchRowViewModel> _branches = [];
    private CompanyBranchRowViewModel? _renameTarget;
    private bool _isBusy;

    public CompanyBranchesWindow(
        CompanyBranchService branchService,
        Guid companyId,
        string companyName)
    {
        _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Company identifier cannot be empty.", nameof(companyId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);
        _companyId = companyId;
        _companyName = companyName;

        InitializeComponent();
        TitleText.Text = $"Filiais — {_companyName}";
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadBranchesAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadBranchesAsync();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateAsync();
    }

    private async void CreateNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateAsync();
        }
    }

    private async Task LoadBranchesAsync(bool preserveFeedback = false)
    {
        if (_isBusy)
        {
            return;
        }

        if (!preserveFeedback)
        {
            HideFeedback();
        }

        CloseRenamePanel();
        SetBusy(true);
        StatusText.Text = "Carregando filiais...";

        try
        {
            var results = await _branchService.ListAsync(_companyId, includeInactive: true);
            _branches = results.Select(CompanyBranchRowViewModel.FromResult).ToList();
            BranchesGrid.ItemsSource = _branches;
            BranchesGrid.Visibility = _branches.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            EmptyText.Visibility = _branches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = _branches.Count == 0
                ? "Nenhuma filial cadastrada."
                : $"{_branches.Count} filial(is) cadastrada(s).";
        }
        catch (AccessDeniedException)
        {
            BranchesGrid.ItemsSource = null;
            EmptyText.Visibility = Visibility.Collapsed;
            StatusText.Text = "Acesso negado.";
            ShowError("Você não tem permissão para consultar filiais.");
        }
        catch (KeyNotFoundException)
        {
            BranchesGrid.ItemsSource = null;
            EmptyText.Visibility = Visibility.Collapsed;
            StatusText.Text = "Empresa não encontrada.";
            ShowError("A empresa selecionada não foi encontrada. Feche esta janela e atualize o cadastro de empresas.");
        }
        catch (Exception exception)
        {
            BranchesGrid.ItemsSource = null;
            EmptyText.Visibility = Visibility.Collapsed;
            StatusText.Text = "Falha ao carregar.";
            Log.Error(exception, "Failed to load company branches for {CompanyId}", _companyId);
            ShowError("Não foi possível carregar as filiais. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AttemptCreateAsync()
    {
        if (_isBusy)
        {
            return;
        }

        HideFeedback();
        SetBusy(true);

        try
        {
            var created = await _branchService.CreateAsync(_companyId, CreateNameTextBox.Text);
            CreateNameTextBox.Clear();
            ShowSuccess($"Filial '{created.Name}' criada com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para criar filiais.");
            return;
        }
        catch (KeyNotFoundException)
        {
            ShowError("A empresa não foi encontrada. Feche esta janela e atualize o cadastro de empresas.");
            return;
        }
        catch (ArgumentException)
        {
            ShowError("Informe um nome de filial válido com até 200 caracteres.");
            return;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create company branch for {CompanyId}", _companyId);
            ShowError("Não foi possível criar a filial. Consulte os logs para detalhes técnicos.");
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await LoadBranchesAsync(preserveFeedback: true);
        CreateNameTextBox.Focus();
    }

    private void OpenRename_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy ||
            sender is not Button button ||
            button.DataContext is not CompanyBranchRowViewModel branch)
        {
            return;
        }

        HideFeedback();
        _renameTarget = branch;
        RenameTitle.Text = $"Renomear — {branch.Name}";
        RenameTextBox.Text = branch.Name;
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
        else if (e.Key == Key.Escape && !_isBusy)
        {
            e.Handled = true;
            CloseRenamePanel();
        }
    }

    private void RenameCancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy)
        {
            CloseRenamePanel();
        }
    }

    private async Task AttemptRenameAsync()
    {
        if (_isBusy || _renameTarget is null)
        {
            return;
        }

        HideFeedback();
        var target = _renameTarget;
        SetBusy(true);

        try
        {
            var updated = await _branchService.RenameAsync(target.Id, RenameTextBox.Text);
            CloseRenamePanel();
            ShowSuccess($"Filial renomeada para '{updated.Name}' com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para renomear filiais.");
            return;
        }
        catch (KeyNotFoundException)
        {
            ShowError("A filial não foi encontrada. Atualize a lista e tente novamente.");
            return;
        }
        catch (ArgumentException)
        {
            ShowError("Informe um nome de filial válido com até 200 caracteres.");
            return;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to rename company branch {CompanyBranchId}", target.Id);
            ShowError("Não foi possível renomear a filial. Consulte os logs para detalhes técnicos.");
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await LoadBranchesAsync(preserveFeedback: true);
    }

    private async void ToggleStatus_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy ||
            sender is not Button button ||
            button.DataContext is not CompanyBranchRowViewModel branch)
        {
            return;
        }

        var activate = !branch.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                this,
                $"Desativar a filial '{branch.Name}'? O registro será preservado e poderá ser reativado depois.",
                "Confirmar desativação de filial",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }

        CloseRenamePanel();
        HideFeedback();
        SetBusy(true);

        try
        {
            var updated = await _branchService.SetActiveAsync(branch.Id, activate);
            ShowSuccess(
                activate
                    ? $"Filial '{updated.Name}' ativada com sucesso."
                    : $"Filial '{updated.Name}' desativada com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para alterar filiais.");
            return;
        }
        catch (KeyNotFoundException)
        {
            ShowError("A filial não foi encontrada. Atualize a lista e tente novamente.");
            return;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change company branch active status {CompanyBranchId}", branch.Id);
            ShowError("Não foi possível alterar o status da filial. Consulte os logs para detalhes técnicos.");
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await LoadBranchesAsync(preserveFeedback: true);
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        RefreshButton.IsEnabled = !isBusy;
        CreateNameTextBox.IsEnabled = !isBusy;
        CreateButton.IsEnabled = !isBusy;
        CreateButton.Content = isBusy ? "Salvando..." : "Criar filial";
        BranchesGrid.IsEnabled = !isBusy;
        RenameTextBox.IsEnabled = !isBusy;
        RenameSaveButton.IsEnabled = !isBusy;
        RenameCancelButton.IsEnabled = !isBusy;
        CloseButton.IsEnabled = !isBusy;
    }

    private void CloseRenamePanel()
    {
        _renameTarget = null;
        RenameTitle.Text = string.Empty;
        RenameTextBox.Clear();
        RenamePanel.Visibility = Visibility.Collapsed;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy)
        {
            Close();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isBusy)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void HideFeedback()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        SuccessPanel.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;
        SuccessText.Text = string.Empty;
    }

    private void ShowError(string message)
    {
        SuccessPanel.Visibility = Visibility.Collapsed;
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void ShowSuccess(string message)
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        SuccessText.Text = message;
        SuccessPanel.Visibility = Visibility.Visible;
    }
}

public sealed record CompanyBranchRowViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativa" : "Inativa";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static CompanyBranchRowViewModel FromResult(CompanyBranchResult branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        return new CompanyBranchRowViewModel(branch.Id, branch.Name, branch.IsActive);
    }
}
