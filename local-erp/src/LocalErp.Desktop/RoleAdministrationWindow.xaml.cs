using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Application.Roles;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using Serilog;

namespace LocalErp.Desktop;

public partial class RoleAdministrationWindow : Window
{
    private readonly RoleService _roleService;
    private List<RoleAdministrationRowViewModel> _roles = [];
    private List<RolePermissionRowViewModel> _permissions = [];
    private Guid? _selectedRoleId;
    private bool _isBusy;

    public RoleAdministrationWindow(RoleService roleService)
    {
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadRolesAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadRolesAsync();
    }

    private async void CreateRole_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateRoleAsync();
    }

    private async void CreateRoleNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateRoleAsync();
        }
    }

    private async Task LoadRolesAsync(bool preserveFeedback = false)
    {
        if (_isBusy)
        {
            return;
        }

        if (!preserveFeedback)
        {
            HideFeedback();
        }

        SetBusy(true);
        RoleListStatusText.Text = "Carregando roles...";

        try
        {
            var results = await _roleService.ListAsync(includeInactive: true);
            _roles = results.Select(RoleAdministrationRowViewModel.FromResult).ToList();
            RolesGrid.ItemsSource = _roles;
            RoleListStatusText.Text = _roles.Count == 0
                ? "Nenhuma role cadastrada."
                : $"{_roles.Count} role(s) cadastrada(s).";

            var selected = _selectedRoleId.HasValue
                ? _roles.FirstOrDefault(item => item.Id == _selectedRoleId.Value)
                : null;
            selected ??= _roles.FirstOrDefault();
            RolesGrid.SelectedItem = selected;
            if (selected is null)
            {
                _selectedRoleId = null;
                ClearPermissionSelection();
            }
        }
        catch (AccessDeniedException)
        {
            RoleListStatusText.Text = "Acesso negado.";
            ShowError("Você não tem permissão para consultar roles e permissões.");
        }
        catch (Exception exception)
        {
            RoleListStatusText.Text = "Falha ao carregar.";
            Log.Error(exception, "Failed to load role administration data");
            ShowError("Não foi possível carregar roles e permissões. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task AttemptCreateRoleAsync()
    {
        if (_isBusy)
        {
            return;
        }

        HideFeedback();
        SetBusy(true);

        try
        {
            var created = await _roleService.CreateAsync(CreateRoleNameTextBox.Text);
            _selectedRoleId = created.Id;
            CreateRoleNameTextBox.Clear();
            ShowSuccess($"Role '{created.Name}' criada com sucesso.");
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para criar roles.");
            return;
        }
        catch (InvalidOperationException)
        {
            ShowError("Já existe uma role com esse nome.");
            return;
        }
        catch (ArgumentException)
        {
            ShowError("Informe um nome de role válido.");
            return;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to create role from administration screen");
            ShowError("Não foi possível criar a role. Consulte os logs para detalhes técnicos.");
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await LoadRolesAsync(preserveFeedback: true);
        CreateRoleNameTextBox.Focus();
    }

    private void RolesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RolesGrid.SelectedItem is not RoleAdministrationRowViewModel role)
        {
            ClearPermissionSelection();
            return;
        }

        _selectedRoleId = role.Id;
        SelectedRoleTitle.Text = $"Permissões — {role.Name}";
        SelectedRoleSubtitle.Text = role.IsActive
            ? "Role ativa. Permissões concedidas produzem efeito para usuários ativos associados."
            : "Role inativa. Seus grants são preservados, mas não produzem capacidade efetiva até a reativação.";
        _permissions = PermissionCatalog.All
            .OrderBy(code => code, StringComparer.Ordinal)
            .Select(code => new RolePermissionRowViewModel(
                code,
                role.Permissions.Contains(code, StringComparer.Ordinal)))
            .ToList();
        PermissionsGrid.ItemsSource = _permissions;
        PermissionsGrid.Visibility = Visibility.Visible;
        NoRoleSelectedText.Visibility = Visibility.Collapsed;
    }

    private async void ToggleRoleStatus_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy ||
            sender is not Button button ||
            button.DataContext is not RoleAdministrationRowViewModel role)
        {
            return;
        }

        var activate = !role.IsActive;
        if (!activate)
        {
            var confirmation = MessageBox.Show(
                this,
                $"Desativar a role '{role.Name}'? As permissões dessa role deixarão de produzir efeito para todos os usuários associados.",
                "Confirmar desativação de role",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }

        HideFeedback();
        SetBusy(true);
        try
        {
            var updated = await _roleService.SetActiveAsync(role.Id, activate);
            _selectedRoleId = updated.Id;
            ShowSuccess(
                activate
                    ? $"Role '{updated.Name}' ativada com sucesso."
                    : $"Role '{updated.Name}' desativada com sucesso.");
        }
        catch (LastAdministrativeRoleCapabilityException)
        {
            ShowError(
                "Esta role não pode ser desativada porque isso eliminaria a última capacidade efetiva de administrar roles no ERP.");
            return;
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para alterar o status de roles.");
            return;
        }
        catch (KeyNotFoundException)
        {
            ShowError("A role não foi encontrada. Atualize a lista e tente novamente.");
            return;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change role active status");
            ShowError("Não foi possível alterar o status da role. Consulte os logs para detalhes técnicos.");
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await LoadRolesAsync(preserveFeedback: true);
    }

    private async void TogglePermission_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy ||
            !_selectedRoleId.HasValue ||
            sender is not Button button ||
            button.DataContext is not RolePermissionRowViewModel permission)
        {
            return;
        }

        var selectedRole = _roles.FirstOrDefault(item => item.Id == _selectedRoleId.Value);
        if (selectedRole is null)
        {
            ShowError("A role selecionada não está mais disponível. Atualize a lista.");
            return;
        }

        if (permission.IsGranted)
        {
            var warning = permission.Code == PermissionCatalog.RolesManage
                ? $"Revogar '{permission.Code}' da role '{selectedRole.Name}'? Esta permissão controla a administração de roles e a operação será bloqueada se for a última capacidade efetiva."
                : $"Revogar '{permission.Code}' da role '{selectedRole.Name}'?";
            var confirmation = MessageBox.Show(
                this,
                warning,
                "Confirmar revogação de permissão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }

        HideFeedback();
        SetBusy(true);
        try
        {
            RoleResult updated;
            if (permission.IsGranted)
            {
                updated = await _roleService.RevokePermissionAsync(selectedRole.Id, permission.Code);
                ShowSuccess($"Permissão '{permission.Code}' revogada da role '{updated.Name}'.");
            }
            else
            {
                updated = await _roleService.GrantPermissionAsync(selectedRole.Id, permission.Code);
                ShowSuccess($"Permissão '{permission.Code}' concedida à role '{updated.Name}'.");
            }

            _selectedRoleId = updated.Id;
        }
        catch (LastAdministrativeRoleCapabilityException)
        {
            ShowError(
                "A permissão roles.manage não pode ser revogada desta role porque isso eliminaria a última capacidade efetiva de administrar roles no ERP.");
            return;
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para alterar permissões de roles.");
            return;
        }
        catch (KeyNotFoundException)
        {
            ShowError("A role não foi encontrada. Atualize a lista e tente novamente.");
            return;
        }
        catch (ArgumentException)
        {
            ShowError("A permissão selecionada não pertence ao catálogo atual do ERP.");
            return;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change role permission grant");
            ShowError("Não foi possível alterar a permissão. Consulte os logs para detalhes técnicos.");
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await LoadRolesAsync(preserveFeedback: true);
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        RefreshButton.IsEnabled = !isBusy;
        CreateRoleNameTextBox.IsEnabled = !isBusy;
        CreateRoleButton.IsEnabled = !isBusy;
        CreateRoleButton.Content = isBusy ? "Salvando..." : "Criar role";
        RolesGrid.IsEnabled = !isBusy;
        PermissionsGrid.IsEnabled = !isBusy;
        CloseButton.IsEnabled = !isBusy;
    }

    private void ClearPermissionSelection()
    {
        SelectedRoleTitle.Text = "Permissões";
        SelectedRoleSubtitle.Text = "Selecione uma role para administrar suas permissões.";
        _permissions = [];
        PermissionsGrid.ItemsSource = null;
        PermissionsGrid.Visibility = Visibility.Collapsed;
        NoRoleSelectedText.Visibility = Visibility.Visible;
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

public sealed record RoleAdministrationRowViewModel(
    Guid Id,
    string Name,
    bool IsActive,
    IReadOnlyList<string> Permissions)
{
    public string Status => IsActive ? "Ativa" : "Inativa";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public int PermissionCount => Permissions.Count;

    public static RoleAdministrationRowViewModel FromResult(RoleResult role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleAdministrationRowViewModel(role.Id, role.Name, role.IsActive, role.Permissions);
    }
}

public sealed record RolePermissionRowViewModel(string Code, bool IsGranted)
{
    public string Status => IsGranted ? "Concedida" : "Não concedida";

    public string ActionLabel => IsGranted ? "Revogar" : "Conceder";
}
