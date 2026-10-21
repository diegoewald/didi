using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalErp.Application.Metadata;
using LocalErp.Application.Security;

namespace LocalErp.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        IUserSession session,
        UsersAdministrationViewModel usersAdministration,
        CompaniesCatalogViewModel companiesCatalog,
        SuppliersCatalogViewModel suppliersCatalog,
        MaterialsCatalogViewModel materialsCatalog,
        ProductsCatalogViewModel productsCatalog,
        MeasurementUnitsCatalogViewModel measurementUnitsCatalog,
        CategoriesCatalogViewModel categoriesCatalog,
        CostCentersCatalogViewModel costCentersCatalog)
    {
        ArgumentNullException.ThrowIfNull(session);
        UsersAdministration = usersAdministration
            ?? throw new ArgumentNullException(nameof(usersAdministration));
        CompaniesCatalog = companiesCatalog
            ?? throw new ArgumentNullException(nameof(companiesCatalog));
        SuppliersCatalog = suppliersCatalog
            ?? throw new ArgumentNullException(nameof(suppliersCatalog));
        MaterialsCatalog = materialsCatalog
            ?? throw new ArgumentNullException(nameof(materialsCatalog));
        ProductsCatalog = productsCatalog
            ?? throw new ArgumentNullException(nameof(productsCatalog));
        MeasurementUnitsCatalog = measurementUnitsCatalog
            ?? throw new ArgumentNullException(nameof(measurementUnitsCatalog));
        CategoriesCatalog = categoriesCatalog
            ?? throw new ArgumentNullException(nameof(categoriesCatalog));
        CostCentersCatalog = costCentersCatalog
            ?? throw new ArgumentNullException(nameof(costCentersCatalog));

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

    public CompaniesCatalogViewModel CompaniesCatalog { get; }

    public SuppliersCatalogViewModel SuppliersCatalog { get; }

    public MaterialsCatalogViewModel MaterialsCatalog { get; }

    public ProductsCatalogViewModel ProductsCatalog { get; }

    public MeasurementUnitsCatalogViewModel MeasurementUnitsCatalog { get; }

    public CategoriesCatalogViewModel CategoriesCatalog { get; }

    public CostCentersCatalogViewModel CostCentersCatalog { get; }

    public string VersionLabel { get; }

    public string CurrentUsername { get; }

    [ObservableProperty]
    private string currentSection;

    [ObservableProperty]
    private bool isCatalogsSelected;

    [ObservableProperty]
    private bool isAdministrationSelected;

    [RelayCommand]
    private async Task NavigateAsync(NavigationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        CurrentSection = item.Title;
        IsCatalogsSelected = string.Equals(
            item.Title,
            "Cadastros",
            StringComparison.Ordinal);
        IsAdministrationSelected = string.Equals(
            item.Title,
            "Administração",
            StringComparison.Ordinal);

        if (IsCatalogsSelected)
        {
            await CompaniesCatalog.LoadAsync();
        }
        else if (IsAdministrationSelected)
        {
            await UsersAdministration.LoadAsync();
        }
    }
}
