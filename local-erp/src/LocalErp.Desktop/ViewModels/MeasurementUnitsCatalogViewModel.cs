using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Security;
using LocalErp.Application.Units;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class MeasurementUnitsCatalogViewModel(MeasurementUnitService measurementUnitService) : ObservableObject
{
    private readonly MeasurementUnitService _measurementUnitService = measurementUnitService
        ?? throw new ArgumentNullException(nameof(measurementUnitService));

    public ObservableCollection<MeasurementUnitListItemViewModel> Units { get; } = [];

    public bool HasUnits => Units.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && Units.Count == 0;

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
            var units = await _measurementUnitService.ListAsync(includeInactive: true);

            Units.Clear();
            foreach (var unit in units)
            {
                Units.Add(MeasurementUnitListItemViewModel.FromResult(unit));
            }
        }
        catch (AccessDeniedException)
        {
            Units.Clear();
            ErrorMessage = "Você não tem permissão para consultar unidades de medida.";
        }
        catch (Exception exception)
        {
            Units.Clear();
            ErrorMessage = "Não foi possível carregar as unidades de medida. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load measurement units catalog");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public void ApplyUnit(MeasurementUnitResult unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        var item = MeasurementUnitListItemViewModel.FromResult(unit);
        var existing = Units.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (existing is not null)
        {
            Units.Remove(existing);
        }

        var insertIndex = 0;
        while (insertIndex < Units.Count && Compare(Units[insertIndex], item) < 0)
        {
            insertIndex++;
        }

        Units.Insert(insertIndex, item);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    private static int Compare(MeasurementUnitListItemViewModel left, MeasurementUnitListItemViewModel right)
    {
        var byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasUnits));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public sealed record MeasurementUnitListItemViewModel(Guid Id, string Name, bool IsActive)
{
    public string Status => IsActive ? "Ativo" : "Inativo";

    public string StatusAction => IsActive ? "Desativar" : "Ativar";

    public static MeasurementUnitListItemViewModel FromResult(MeasurementUnitResult unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return new MeasurementUnitListItemViewModel(unit.Id, unit.Name, unit.IsActive);
    }
}
