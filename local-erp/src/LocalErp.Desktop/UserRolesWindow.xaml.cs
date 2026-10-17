using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LocalErp.Application.Roles;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop;

public partial class UserRolesWindow : Window
{
    private readonly RoleService _roleService;
    private readonly Guid _userId;
    private readonly bool _userIsActive;
    private List<UserRoleAssignmentRowViewModel> _rows = [];
    private bool _isChangingAssignment;

    public UserRolesWindow(
        RoleService roleService,
        Guid userId,
        string username,
        bool userIsActive)
    {
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        _userId = userId;
        _userIsActive = userIsActive;

        InitializeComponent();
        TargetTitle.Text = $"Roles — {username}";
        TargetSubtitle.Text = userIsActive
            ? "Associe ou remova roles da conta selecionada. As permissões continuam sendo validadas pela camada de aplicação."
            : "A conta está inativa. As associações existentes podem ser removidas, mas novas roles só podem ser atribuídas após a reativação.";
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAssignmentsAsync();
    }

    private async Task LoadAssignmentsAsync()
    {
        HideFeedback();
        LoadingPanel.Visibility = Visibility.Visible;
        EmptyPanel.Visibility = Visibility.Collapsed;
        RolesPanel.Visibility = Visibility.Collapsed;

        try
        {
            var assignments = await _roleService.ListUserAssignmentsAsync(_userId);
            _rows = assignments
                .Select(item => new UserRoleAssignmentRowViewModel(
                    item.RoleId,
                    item.RoleName,
                    item.RoleIsActive,
                    item.IsAssigned,
                    _userIsActive))
                .ToList();

            RolesGrid.ItemsSource = _rows;
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RolesPanel.Visibility = _rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (AccessDeniedException)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ShowError("Você não tem permissão para consultar as roles deste usuário.");
        }
        catch (KeyNotFoundException)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ShowError("O usuário não foi encontrado. Feche esta janela, atualize a lista e tente novamente.");
        }
        catch (Exception exception)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            Log.Error(exception, "Failed to load user role assignments");
            ShowError("Não foi possível carregar as roles. Consulte os logs para detalhes técnicos.");
        }
    }

    private async void ToggleAssignment_Click(object sender, RoutedEventArgs e)
    {
        if (_isChangingAssignment ||
            sender is not Button button ||
            button.DataContext is not UserRoleAssignmentRowViewModel row)
        {
            return;
        }

        if (row.IsAssigned)
        {
            var confirmation = MessageBox.Show(
                this,
                $"Remover a role '{row.RoleName}' deste usuário?",
                "Confirmar remoção de role",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }

        HideFeedback();
        _isChangingAssignment = true;
        CloseButton.IsEnabled = false;
        button.IsEnabled = false;
        button.Content = "Salvando...";

        try
        {
            if (row.IsAssigned)
            {
                await _roleService.RemoveUserAsync(_userId, row.RoleId);
                ApplyAssignmentState(row.RoleId, isAssigned: false);
                ShowSuccess($"Role '{row.RoleName}' removida com sucesso.");
            }
            else
            {
                await _roleService.AssignUserAsync(_userId, row.RoleId);
                ApplyAssignmentState(row.RoleId, isAssigned: true);
                ShowSuccess($"Role '{row.RoleName}' atribuída com sucesso.");
            }
        }
        catch (LastAdministrativeRoleAssignmentException)
        {
            ShowError(
                "Esta associação não pode ser removida porque ela eliminaria a última capacidade efetiva de administrar roles no ERP.");
        }
        catch (AccessDeniedException)
        {
            ShowError("Você não tem permissão para alterar associações de roles.");
        }
        catch (KeyNotFoundException)
        {
            ShowError("O usuário ou a role não foi encontrado. Atualize os dados e tente novamente.");
        }
        catch (InvalidOperationException)
        {
            ShowError("A associação não pode ser criada porque o usuário ou a role está inativo.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to change user role assignment");
            ShowError("Não foi possível alterar a associação de role. Consulte os logs para detalhes técnicos.");
        }
        finally
        {
            _isChangingAssignment = false;
            CloseButton.IsEnabled = true;
            button.IsEnabled = row.CanChange;
            button.Content = row.ActionLabel;
        }
    }

    private void ApplyAssignmentState(Guid roleId, bool isAssigned)
    {
        var index = _rows.FindIndex(item => item.RoleId == roleId);
        if (index < 0)
        {
            return;
        }

        _rows[index] = _rows[index] with { IsAssigned = isAssigned };
        RolesGrid.ItemsSource = null;
        RolesGrid.ItemsSource = _rows;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (!_isChangingAssignment)
        {
            Close();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isChangingAssignment)
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

public sealed record UserRoleAssignmentRowViewModel(
    Guid RoleId,
    string RoleName,
    bool RoleIsActive,
    bool IsAssigned,
    bool UserIsActive)
{
    public string RoleStatus => RoleIsActive ? "Ativa" : "Inativa";

    public string AssignmentStatus => IsAssigned ? "Atribuída" : "Não atribuída";

    public string ActionLabel => IsAssigned ? "Remover" : "Adicionar";

    public bool CanChange => IsAssigned || (UserIsActive && RoleIsActive);
}
