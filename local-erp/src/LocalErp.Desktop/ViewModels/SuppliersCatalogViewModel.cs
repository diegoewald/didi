using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Suppliers;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class SuppliersCatalogViewModel(SupplierService supplierService) : ObservableObject
{
    private readonly SupplierService _supplierService = supplierService
        ?? throw new ArgumentNullException(nameof(supplierService));

    public ObservableCollection<SupplierListItemViewModel> Suppliers { get; } = [];

    public bool HasSuppliers => Suppliers.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Suppliers.Count == 0;

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
            var suppliers = await _supplierService.ListAsync(includeInactive: true);

            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(SupplierListItemViewModel.FromResult(supplier));
            }
        }
        catch (AccessDeniedException)
        {
            Suppliers.Clear();
            ErrorMessage = "Você não tem permissão para consultar fornecedores.";
        }
        catch (Exception exception)
        {
            Suppliers.Clear();
            ErrorMessage = "Não foi possível carregar os fornecedores. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load suppliers catalog");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public void ApplySupplier(SupplierResult supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        var item = SupplierListItemViewModel.FromResult(supplier);
        var existing = Suppliers.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            Suppliers.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < Suppliers.Count && Compare(Suppliers[insertIndex], item) < 0)
        {
            insertIndex++;
        }

        Suppliers.Insert(insertIndex, item);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private static int Compare(SupplierListItemViewModel left, SupplierListItemViewModel right)
    {
        var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasSuppliers));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record SupplierListItemViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativo" : "Inativo";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static SupplierListItemViewModel FromResult(SupplierResult supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        return new SupplierListItemViewModel(supplier.Id, supplier.Name, supplier.IsActive);
    }
}
