using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class PurchaseRequestsWorkspaceViewModel : ObservableObject
{
    private readonly PurchaseRequestService _purchaseRequestService;

    public PurchaseRequestsWorkspaceViewModel(PurchaseRequestService purchaseRequestService)
    {
        _purchaseRequestService = purchaseRequestService
            ?? throw new ArgumentNullException(nameof(purchaseRequestService));
        StatusFilters =
        [
            new("Todos", null),
            new("Rascunho", PurchaseRequestStatus.Draft),
            new("Enviada", PurchaseRequestStatus.Submitted),
            new("Cancelada", PurchaseRequestStatus.Cancelled),
        ];
        selectedStatusFilter = StatusFilters[0];
    }

    public ObservableCollection<PurchaseRequestListItemViewModel> PurchaseRequests { get; } = [];

    public ObservableCollection<PurchaseRequestMeasurementUnitOption> MeasurementUnits { get; } = [];

    public ObservableCollection<PurchaseRequestMeasurementUnitOption> ActiveMeasurementUnits { get; } = [];

    public ObservableCollection<PurchaseRequestLineListItemViewModel> CurrentLines { get; } = [];

    public IReadOnlyList<PurchaseRequestStatusFilterOption> StatusFilters { get; }

    [ObservableProperty]
    private PurchaseRequestStatusFilterOption selectedStatusFilter;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasLoaded;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private PurchaseRequestResult? currentRequest;

    public bool HasRequests => PurchaseRequests.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && PurchaseRequests.Count == 0;

    public bool HasCurrentRequest => CurrentRequest is not null;

    public bool IsDraft => CurrentRequest?.Status == PurchaseRequestStatus.Draft;

    public bool IsReadOnly => CurrentRequest is not null && !IsDraft;

    public bool CanCancel => CurrentRequest is { Status: not PurchaseRequestStatus.Cancelled };

    public string CurrentStatusLabel => CurrentRequest is null ? string.Empty : StatusLabel(CurrentRequest.Status);

    public string CurrentCreatedDisplay => CurrentRequest is null
        ? string.Empty
        : CurrentRequest.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

    public string CurrentIdDisplay => CurrentRequest?.Id.ToString("D") ?? string.Empty;

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
            var requests = await _purchaseRequestService.ListAsync(SelectedStatusFilter.Status);
            var units = await _purchaseRequestService.ListMeasurementUnitsAsync();
            ReplaceMeasurementUnits(units);

            PurchaseRequests.Clear();
            foreach (var request in requests
                         .OrderByDescending(item => item.CreatedAtUtc)
                         .ThenBy(item => item.Id))
            {
                PurchaseRequests.Add(PurchaseRequestListItemViewModel.FromResult(request));
            }

            if (CurrentRequest is not null)
            {
                if (PurchaseRequests.Any(item => item.Id == CurrentRequest.Id))
                {
                    var refreshed = await _purchaseRequestService.GetByIdAsync(CurrentRequest.Id);
                    SetCurrent(refreshed);
                }
                else
                {
                    ClearCurrent();
                }
            }
        }
        catch (AccessDeniedException)
        {
            PurchaseRequests.Clear();
            MeasurementUnits.Clear();
            ActiveMeasurementUnits.Clear();
            ClearCurrent();
            ErrorMessage = "Você não tem permissão para consultar solicitações de compra.";
        }
        catch (Exception exception)
        {
            PurchaseRequests.Clear();
            ClearCurrent();
            ErrorMessage = "Não foi possível carregar as solicitações de compra. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load purchase requests workspace");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public async Task SelectAsync(Guid purchaseRequestId)
    {
        SetCurrent(await _purchaseRequestService.GetByIdAsync(purchaseRequestId));
    }

    public async Task EnsureMeasurementUnitsAsync()
    {
        if (MeasurementUnits.Count > 0)
        {
            return;
        }

        ReplaceMeasurementUnits(await _purchaseRequestService.ListMeasurementUnitsAsync());
    }

    public async Task<PurchaseRequestResult> CreateDraftAsync()
    {
        var result = await _purchaseRequestService.CreateDraftAsync();
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseRequestResult> UpdateNotesAsync(string? notes)
    {
        var current = RequireCurrent();
        var result = await _purchaseRequestService.UpdateDraftAsync(current.Id, notes);
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseRequestResult> AddLineAsync(
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate)
    {
        var current = RequireCurrent();
        var result = await _purchaseRequestService.AddLineAsync(
            current.Id,
            description,
            quantity,
            measurementUnitId,
            neededByDate);
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseRequestResult> UpdateLineAsync(
        Guid lineId,
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate)
    {
        var current = RequireCurrent();
        var result = await _purchaseRequestService.UpdateLineAsync(
            current.Id,
            lineId,
            description,
            quantity,
            measurementUnitId,
            neededByDate);
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseRequestResult> RemoveLineAsync(Guid lineId)
    {
        var current = RequireCurrent();
        var result = await _purchaseRequestService.RemoveLineAsync(current.Id, lineId);
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseRequestResult> SubmitAsync()
    {
        var current = RequireCurrent();
        var result = await _purchaseRequestService.SubmitAsync(current.Id);
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseRequestResult> CancelAsync(string reason)
    {
        var current = RequireCurrent();
        var result = await _purchaseRequestService.CancelAsync(current.Id, reason);
        ApplyResult(result);
        return result;
    }

    public void ClearCurrent()
    {
        CurrentRequest = null;
        CurrentLines.Clear();
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    partial void OnCurrentRequestChanged(PurchaseRequestResult? value) => NotifyDerivedState();

    private PurchaseRequestResult RequireCurrent()
    {
        return CurrentRequest ?? throw new InvalidOperationException("No purchase request is selected.");
    }

    private void ApplyResult(PurchaseRequestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var existing = PurchaseRequests.FirstOrDefault(item => item.Id == result.Id);
        if (existing is not null)
        {
            PurchaseRequests.Remove(existing);
        }

        if (MatchesCurrentFilter(result))
        {
            var item = PurchaseRequestListItemViewModel.FromResult(result);
            var insertIndex = 0;
            while (insertIndex < PurchaseRequests.Count && Compare(PurchaseRequests[insertIndex], item) < 0)
            {
                insertIndex++;
            }

            PurchaseRequests.Insert(insertIndex, item);
        }

        SetCurrent(result);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    private void SetCurrent(PurchaseRequestResult result)
    {
        CurrentRequest = result ?? throw new ArgumentNullException(nameof(result));
        RebuildLines();
        NotifyDerivedState();
    }

    private void ReplaceMeasurementUnits(IReadOnlyList<PurchaseRequestMeasurementUnitOption> units)
    {
        MeasurementUnits.Clear();
        ActiveMeasurementUnits.Clear();

        foreach (var unit in units)
        {
            MeasurementUnits.Add(unit);
            if (unit.IsActive)
            {
                ActiveMeasurementUnits.Add(unit);
            }
        }

        RebuildLines();
    }

    private void RebuildLines()
    {
        CurrentLines.Clear();
        if (CurrentRequest is null)
        {
            return;
        }

        foreach (var line in CurrentRequest.Lines.OrderBy(item => item.LineNumber))
        {
            var unitName = MeasurementUnits.FirstOrDefault(unit => unit.Id == line.MeasurementUnitId)?.Name
                ?? "Unidade não encontrada";
            CurrentLines.Add(PurchaseRequestLineListItemViewModel.FromResult(line, unitName));
        }
    }

    private bool MatchesCurrentFilter(PurchaseRequestResult result)
    {
        return SelectedStatusFilter.Status is null || SelectedStatusFilter.Status == result.Status;
    }

    private static int Compare(PurchaseRequestListItemViewModel left, PurchaseRequestListItemViewModel right)
    {
        var byDate = right.CreatedAtUtc.CompareTo(left.CreatedAtUtc);
        return byDate != 0 ? byDate : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasRequests));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasCurrentRequest));
        OnPropertyChanged(nameof(IsDraft));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CurrentStatusLabel));
        OnPropertyChanged(nameof(CurrentCreatedDisplay));
        OnPropertyChanged(nameof(CurrentIdDisplay));
    }

    public static string StatusLabel(PurchaseRequestStatus status) => status switch
    {
        PurchaseRequestStatus.Draft => "Rascunho",
        PurchaseRequestStatus.Submitted => "Enviada",
        PurchaseRequestStatus.Cancelled => "Cancelada",
        _ => status.ToString(),
    };
}

public sealed record PurchaseRequestStatusFilterOption(string Label, PurchaseRequestStatus? Status);

public sealed record PurchaseRequestListItemViewModel(
    Guid Id,
    PurchaseRequestStatus Status,
    DateTime CreatedAtUtc,
    int LineCount,
    string? Notes)
{
    public string StatusLabel => PurchaseRequestsWorkspaceViewModel.StatusLabel(Status);

    public string CreatedDisplay => CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "—" : Notes;

    public static PurchaseRequestListItemViewModel FromResult(PurchaseRequestResult request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new PurchaseRequestListItemViewModel(
            request.Id,
            request.Status,
            request.CreatedAtUtc,
            request.Lines.Count,
            request.Notes);
    }
}

public sealed record PurchaseRequestLineListItemViewModel(
    Guid Id,
    int LineNumber,
    string Description,
    decimal Quantity,
    Guid MeasurementUnitId,
    string MeasurementUnitName,
    DateOnly? NeededByDate)
{
    public string QuantityDisplay => Quantity.ToString("0.####", CultureInfo.CurrentCulture);

    public string NeededByDateDisplay => NeededByDate?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";

    public static PurchaseRequestLineListItemViewModel FromResult(
        PurchaseRequestLineResult line,
        string measurementUnitName)
    {
        ArgumentNullException.ThrowIfNull(line);
        return new PurchaseRequestLineListItemViewModel(
            line.Id,
            line.LineNumber,
            line.Description,
            line.Quantity,
            line.MeasurementUnitId,
            measurementUnitName,
            line.NeededByDate);
    }
}
