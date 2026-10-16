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

    public string PasswordPolicyHint =>
        $"Use entre {PasswordPolicy.MinimumLength} e {PasswordPolicy.MaximumLength} caracteres.";

    public bool HasUsers => Users.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Users.Count == 0;

    public bool HasCreateError => !string.IsNullOrWhiteSpace(CreateErrorMessage);

    public bool HasCreateSuccess => !string.IsNullOrWhiteSpace(CreateSuccessMessage);

    public bool CanCreateUser => !IsCreating;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasLoaded;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isCreating;

    [ObservableProperty]
    private string? createErrorMessage;

    [ObservableProperty]
    private string? createSuccessMessage;

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

    public async Task<bool> CreateUserAsync(
        string username,
        string password,
        string passwordConfirmation)
    {
        if (IsCreating)
        {
            return false;
        }

        CreateErrorMessage = null;
        CreateSuccessMessage = null;

        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
        {
            CreateErrorMessage = "As senhas informadas não coincidem.";
            return false;
        }

        IsCreating = true;

        try
        {
            var created = await _userService.CreateAsync(username, password);
            AddOrReplaceUser(created);
            CreateSuccessMessage = $"Usuário '{created.Username}' criado com sucesso.";
            return true;
        }
        catch (AccessDeniedException)
        {
            CreateErrorMessage = "Você não tem permissão para criar usuários.";
        }
        catch (InvalidOperationException)
        {
            CreateErrorMessage = "Já existe um usuário com esse nome.";
        }
        catch (ArgumentException)
        {
            CreateErrorMessage = "Verifique o nome de usuário e a política da senha.";
        }
        catch (Exception exception)
        {
            CreateErrorMessage = "Não foi possível criar o usuário. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to create user from administration screen");
        }
        finally
        {
            IsCreating = false;
            NotifyCreateState();
        }

        return false;
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    partial void OnIsCreatingChanged(bool value) => NotifyCreateState();

    partial void OnCreateErrorMessageChanged(string? value) => NotifyCreateState();

    partial void OnCreateSuccessMessageChanged(string? value) => NotifyCreateState();

    private void AddOrReplaceUser(UserResult user)
    {
        var item = UserListItemViewModel.FromResult(user);
        var existing = Users.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            Users.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < Users.Count &&
               string.Compare(Users[insertIndex].Username, item.Username, StringComparison.OrdinalIgnoreCase) < 0)
        {
            insertIndex++;
        }

        Users.Insert(insertIndex, item);
        HasLoaded = true;
        NotifyDerivedState();
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasUsers));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void NotifyCreateState()
    {
        OnPropertyChanged(nameof(HasCreateError));
        OnPropertyChanged(nameof(HasCreateSuccess));
        OnPropertyChanged(nameof(CanCreateUser));
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
