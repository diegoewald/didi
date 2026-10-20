using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Categories;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class CategoriesCatalogViewModel(CategoryService categoryService) : ObservableObject
{
    private readonly CategoryService _categoryService = categoryService
        ?? throw new ArgumentNullException(nameof(categoryService));

    public ObservableCollection<CategoryListItemViewModel> Categories { get; } = [];

    public bool HasCategories => Categories.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Categories.Count == 0;

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
            var categories = await _categoryService.ListAsync(includeInactive: true);

            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(CategoryListItemViewModel.FromResult(category));
            }
        }
        catch (AccessDeniedException)
        {
            Categories.Clear();
            ErrorMessage = "Você não tem permissão para consultar categorias.";
        }
        catch (Exception exception)
        {
            Categories.Clear();
            ErrorMessage = "Não foi possível carregar as categorias. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load categories catalog");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public void ApplyCategory(CategoryResult category)
    {
        ArgumentNullException.ThrowIfNull(category);

        var item = CategoryListItemViewModel.FromResult(category);
        var existing = Categories.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            Categories.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < Categories.Count && Compare(Categories[insertIndex], item) < 0)
        {
            insertIndex++;
        }

        Categories.Insert(insertIndex, item);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private static int Compare(CategoryListItemViewModel left, CategoryListItemViewModel right)
    {
        var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record CategoryListItemViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativo" : "Inativo";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static CategoryListItemViewModel FromResult(CategoryResult category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return new CategoryListItemViewModel(category.Id, category.Name, category.IsActive);
    }
}
