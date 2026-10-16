using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class UsersAdministrationViewModel(UserService userService) : ObservableObject
{
    private readonly UserService _userService = userService
        ?? throw new ArgumentNullException(nameof(userService));

    public ObservableCollection<UserListItemViewModel> Users { get; } = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasLoaded;

    [ObservableProperty]
    private string? errorMessage;

    public bool HasUsers => Users.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Users.Count == 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var users = await _userService.ListAsync(includeInactive: true);

            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(UserListItemViewModel.FromResult(user));
            }
        }
        catch (AccessDeniedException)
        {
            Users.Clear();
            ErrorMessage = "Você não tem permissão para consultar usuários.";
        }
        catch (Exception exception)
        {
            Users.Clear();
            ErrorMessage = "Não foi possível carregar os usuários. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load user administration list");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasUsers));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record UserListItemViewModel(Guid Id, string Username, bool IsActive)
{
    public string Status => IsActive ? "Ativo" : "Inativo";

    public static UserListItemViewModel FromResult(UserResult user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserListItemViewModel(user.Id, user.Username, user.IsActive);
    }
}
