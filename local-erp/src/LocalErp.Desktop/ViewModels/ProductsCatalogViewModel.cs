using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Products;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class ProductsCatalogViewModel(ProductService productService) : ObservableObject
{
    private readonly ProductService _productService = productService
        ?? throw new ArgumentNullException(nameof(productService));

    public ObservableCollection<ProductListItemViewModel> Products { get; } = [];

    public bool HasProducts => Products.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Products.Count == 0;

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
            var products = await _productService.ListAsync(includeInactive: true);

            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(ProductListItemViewModel.FromResult(product));
            }
        }
        catch (AccessDeniedException)
        {
            Products.Clear();
            ErrorMessage = "Você não tem permissão para consultar produtos.";
        }
        catch (Exception exception)
        {
            Products.Clear();
            ErrorMessage = "Não foi possível carregar os produtos. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load products catalog");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public void ApplyProduct(ProductResult product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var item = ProductListItemViewModel.FromResult(product);
        var existing = Products.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            Products.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < Products.Count && Compare(Products[insertIndex], item) < 0)
        {
            insertIndex++;
        }

        Products.Insert(insertIndex, item);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private static int Compare(ProductListItemViewModel left, ProductListItemViewModel right)
    {
        var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasProducts));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record ProductListItemViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativo" : "Inativo";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static ProductListItemViewModel FromResult(ProductResult product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return new ProductListItemViewModel(product.Id, product.Name, product.IsActive);
    }
}
