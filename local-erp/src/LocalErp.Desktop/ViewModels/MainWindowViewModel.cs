using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalErp.Application.Metadata;
using LocalErp.Application.Security;

namespace LocalErp.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        IUserSession session,
        UsersAdministrationViewModel usersAdministration)
    {
        ArgumentNullException.ThrowIfNull(session);
        UsersAdministration = usersAdministration
            ?? throw new ArgumentNullException(nameof(usersAdministration));

        NavigationItems =
        [
            new("Visão geral", "\uE80F"),
            new("Cadastros", "\uE8D4"),
            new("Compras", "\uE719"),
            new("Estoque", "\uE7BF"),
            new("Vendas", "\uE8A1"),
            new("Financeiro", "\uE8C7"),
            new("Relatórios", "\uE9D2"),
            new("Administração", "\uE713")
        ];

        currentSection = NavigationItems[0].Title;
        VersionLabel = $"Versão {ApplicationMetadata.Version}";
        CurrentUsername = session.CurrentUser?.Username ?? "Usuário";
    }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public UsersAdministrationViewModel UsersAdministration { get; }

    public string VersionLabel { get; }

    public string CurrentUsername { get; }

    [ObservableProperty]
    private string currentSection;

    [ObservableProperty]
    private bool isAdministrationSelected;

    [RelayCommand]
    private async Task NavigateAsync(NavigationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CurrentSection = item.Title;
        IsAdministrationSelected = string.Equals(
            item.Title,
            "Administração",
            StringComparison.Ordinal);

        if (IsAdministrationSelected)
        {
            await UsersAdministration.LoadAsync();
        }
    }
}
