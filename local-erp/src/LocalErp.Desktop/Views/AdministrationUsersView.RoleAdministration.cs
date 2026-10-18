using System.Windows;
using System.Windows.Controls;

namespace LocalErp.Desktop.Views;

public partial class AdministrationUsersView
{
    private void OpenRolesAdministration_Click(object sender, RoutedEventArgs e)
    {
        if (_isCreating ||
            _isChangingStatus ||
            _isResettingPassword ||
            _roleService is null)
        {
            return;
        }

        ClosePasswordResetPanel();
        HideStatusMessages();

        var window = new RoleAdministrationWindow(_roleService);
        var owner = Window.GetWindow(this);
        if (owner is not null)
        {
            window.Owner = owner;
        }

        window.ShowDialog();
    }
}
