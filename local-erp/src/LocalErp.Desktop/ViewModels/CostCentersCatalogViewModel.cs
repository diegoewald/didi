using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.CostCenters;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class CostCentersCatalogViewModel(CostCenterService costCenterService) : ObservableObject
{
    private readonly CostCenterService _costCenterService = costCenterService
        ?? throw new ArgumentNullException(nameof(costCenterService));

    public ObservableCollection<CostCenterListItemViewModel> CostCenters { get; } = [];

    public bool HasCostCenters => CostCenters.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && CostCenters.Count == 0;

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
            var costCenters = await _costCenterService.ListAsync(includeInactive: true);

            CostCenters.Clear();
            foreach (var costCenter in costCenters
                         .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(item => item.Id))
            {
                CostCenters.Add(CostCenterListItemViewModel.FromResult(costCenter));
            }
        }
        catch (AccessDeniedException)
        {
            CostCenters.Clear();
            ErrorMessage = "Você não tem permissão para consultar centros de custo.";
        }
        catch (Exception exception)
        {
            CostCenters.Clear();
            ErrorMessage = "Não foi possível carregar os centros de custo. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load cost centers catalog");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public void ApplyCostCenter(CostCenterResult costCenter)
    {
        ArgumentNullException.ThrowIfNull(costCenter);

        var item = CostCenterListItemViewModel.FromResult(costCenter);
        var existing = CostCenters.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            CostCenters.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < CostCenters.Count && Compare(CostCenters[insertIndex], item) < 0)
        {
            insertIndex++;
        }

        CostCenters.Insert(insertIndex, item);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private static int Compare(CostCenterListItemViewModel left, CostCenterListItemViewModel right)
    {
        var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasCostCenters));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record CostCenterListItemViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativo" : "Inativo";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static CostCenterListItemViewModel FromResult(CostCenterResult costCenter)
    {
        ArgumentNullException.ThrowIfNull(costCenter);
        return new CostCenterListItemViewModel(costCenter.Id, costCenter.Name, costCenter.IsActive);
    }
}
