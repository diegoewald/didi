using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Companies;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class CompaniesCatalogViewModel(CompanyService companyService) : ObservableObject
{
    private readonly CompanyService _companyService = companyService
        ?? throw new ArgumentNullException(nameof(companyService));

    public ObservableCollection<CompanyListItemViewModel> Companies { get; } = [];

    public bool HasCompanies => Companies.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Companies.Count == 0;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasLoaded;

    [ObservableProperty]
    private string? errorMessage;

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
            var companies = await _companyService.ListAsync(includeInactive: true);

            Companies.Clear();
            foreach (var company in companies)
            {
                Companies.Add(CompanyListItemViewModel.FromResult(company));
            }
        }
        catch (AccessDeniedException)
        {
            Companies.Clear();
            ErrorMessage = "Você não tem permissão para consultar empresas.";
        }
        catch (Exception exception)
        {
            Companies.Clear();
            ErrorMessage = "Não foi possível carregar as empresas. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load companies catalog");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public void ApplyCompany(CompanyResult company)
    {
        ArgumentNullException.ThrowIfNull(company);

        var item = CompanyListItemViewModel.FromResult(company);
        var existing = Companies.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            Companies.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < Companies.Count && Compare(Companies[insertIndex], item) < 0)
        {
            insertIndex++;
        }

        Companies.Insert(insertIndex, item);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private static int Compare(CompanyListItemViewModel left, CompanyListItemViewModel right)
    {
        var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasCompanies));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record CompanyListItemViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativa" : "Inativa";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static CompanyListItemViewModel FromResult(CompanyResult company)
    {
        ArgumentNullException.ThrowIfNull(company);
        return new CompanyListItemViewModel(company.Id, company.Name, company.IsActive);
    }
}
