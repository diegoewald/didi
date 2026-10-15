using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalErp.Application.Metadata;

namespace LocalErp.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel()
    {
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
    }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public string VersionLabel { get; }

    [ObservableProperty]
    private string currentSection;

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        CurrentSection = item.Title;
    }
}
