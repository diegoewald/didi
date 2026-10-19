using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Materials;
using LocalErp.Application.Security;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class MaterialsCatalogViewModel(MaterialService materialService) : ObservableObject
{
    private readonly MaterialService _materialService = materialService
        ?? throw new ArgumentNullException(nameof(materialService));

    public ObservableCollection<MaterialListItemViewModel> Materials { get; } = [];

    public bool HasMaterials => Materials.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Materials.Count == 0;

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
            var materials = await _materialService.ListAsync(includeInactive: true);

            Materials.Clear();
            foreach (var material in materials)
            {
                Materials.Add(MaterialListItemViewModel.FromResult(material));
            }
        }
        catch (AccessDeniedException)
        {
            Materials.Clear();
            ErrorMessage = "Você não tem permissão para consultar materiais.";
        }
        catch (Exception exception)
        {
            Materials.Clear();
            ErrorMessage = "Não foi possível carregar os materiais. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load materials catalog");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public void ApplyMaterial(MaterialResult material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var item = MaterialListItemViewModel.FromResult(material);
        var existing = Materials.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            Materials.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < Materials.Count && Compare(Materials[insertIndex], item) < 0)
        {
            insertIndex++;
        }

        Materials.Insert(insertIndex, item);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private static int Compare(MaterialListItemViewModel left, MaterialListItemViewModel right)
    {
        var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasMaterials));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record MaterialListItemViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativo" : "Inativo";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static MaterialListItemViewModel FromResult(MaterialResult material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return new MaterialListItemViewModel(material.Id, material.Name, material.IsActive);
    }
}
