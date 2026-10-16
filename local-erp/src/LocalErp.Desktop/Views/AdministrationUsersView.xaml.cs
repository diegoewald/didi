using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LocalErp.Desktop.ViewModels;

namespace LocalErp.Desktop.Views;

public partial class AdministrationUsersView : UserControl
{
    public AdministrationUsersView()
    {
        InitializeComponent();
    }

    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        await AttemptCreateUserAsync();
    }

    private async void ConfirmPasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AttemptCreateUserAsync();
        }
    }

    private async Task AttemptCreateUserAsync()
    {
        if (DataContext is not UsersAdministrationViewModel viewModel)
        {
            return;
        }

        var password = CreatePasswordInput.Password;
        var passwordConfirmation = ConfirmPasswordInput.Password;

        try
        {
            var created = await viewModel.CreateUserAsync(
                CreateUsernameTextBox.Text,
                password,
                passwordConfirmation);

            if (created)
            {
                CreateUsernameTextBox.Clear();
                CreateUsernameTextBox.Focus();
            }
            else
            {
                CreatePasswordInput.Focus();
            }
        }
        finally
        {
            CreatePasswordInput.Clear();
            ConfirmPasswordInput.Clear();
        }
    }
}
