using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class PurchaseOrdersWorkspaceViewModel : ObservableObject
{
    private readonly PurchaseOrderService _purchaseOrderService;
    private readonly PurchaseOrderCreationLookupService _creationLookupService;

    public PurchaseOrdersWorkspaceViewModel(
        PurchaseOrderService purchaseOrderService,
        PurchaseOrderCreationLookupService creationLookupService)
    {
        _purchaseOrderService = purchaseOrderService
            ?? throw new ArgumentNullException(nameof(purchaseOrderService));
        _creationLookupService = creationLookupService
            ?? throw new ArgumentNullException(nameof(creationLookupService));
        StatusFilters =
        [
            new("Todos", null),
            new("Rascunho", PurchaseOrderStatus.Draft),
            new("Emitido", PurchaseOrderStatus.Issued),
            new("Cancelado", PurchaseOrderStatus.Cancelled),
        ];
        selectedStatusFilter = StatusFilters[0];
    }

    public ObservableCollection<PurchaseOrderListItemViewModel> PurchaseOrders { get; } = [];

    public ObservableCollection<PurchaseOrderLineListItemViewModel> CurrentLines { get; } = [];

    public ObservableCollection<PurchaseOrderCreationCandidateViewModel> CreationCandidates { get; } = [];

    public IReadOnlyList<PurchaseOrderStatusFilterOption> StatusFilters { get; }

    [ObservableProperty]
    private PurchaseOrderStatusFilterOption selectedStatusFilter;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasLoaded;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private PurchaseOrderResult? currentPurchaseOrder;

    public bool HasPurchaseOrders => PurchaseOrders.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && PurchaseOrders.Count == 0;

    public bool HasCurrentPurchaseOrder => CurrentPurchaseOrder is not null;

    public bool IsDraft => CurrentPurchaseOrder?.Status == PurchaseOrderStatus.Draft;

    public bool IsIssued => CurrentPurchaseOrder?.Status == PurchaseOrderStatus.Issued;

    public bool HasIssuedHistory => CurrentPurchaseOrder?.IssuedAtUtc is not null;

    public bool IsCancelled => CurrentPurchaseOrder?.Status == PurchaseOrderStatus.Cancelled;

    public bool IsReadOnly => CurrentPurchaseOrder is not null && !IsDraft;

    public bool CanIssue => IsDraft;

    public bool CanCancel => CurrentPurchaseOrder?.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Issued;

    public string CurrentNumberDisplay => CurrentPurchaseOrder is null
        ? string.Empty
        : FormatNumber(CurrentPurchaseOrder.SequenceNumber);

    public string CurrentStatusLabel => CurrentPurchaseOrder is null
        ? string.Empty
        : StatusLabel(CurrentPurchaseOrder.Status);

    public string CurrentCreatedDisplay => CurrentPurchaseOrder is null
        ? string.Empty
        : CurrentPurchaseOrder.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

    public string CurrentSupplierDisplay => CurrentPurchaseOrder?.SupplierNameSnapshot ?? string.Empty;

    public string CurrentCurrencyDisplay => CurrentPurchaseOrder?.CurrencyCode ?? string.Empty;

    public string CurrentTotalDisplay => CurrentPurchaseOrder is null
        ? string.Empty
        : FormatMoney(CurrentPurchaseOrder.Total, CurrentPurchaseOrder.CurrencyCode);

    public string CurrentAwardIdDisplay => CurrentPurchaseOrder?.RequestForQuotationAwardId.ToString("D") ?? string.Empty;

    public string CurrentIssuedDisplay => CurrentPurchaseOrder?.IssuedAtUtc is DateTime issuedAtUtc
        ? issuedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)
        : "—";

    public string CurrentCancelledDisplay => CurrentPurchaseOrder?.CancelledAtUtc is DateTime cancelledAtUtc
        ? cancelledAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)
        : "—";

    public string CurrentCancellationReasonDisplay => string.IsNullOrWhiteSpace(CurrentPurchaseOrder?.CancellationReason)
        ? "—"
        : CurrentPurchaseOrder.CancellationReason;

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
            var purchaseOrders = await _purchaseOrderService.ListAsync(SelectedStatusFilter.Status);
            PurchaseOrders.Clear();
            foreach (var purchaseOrder in purchaseOrders)
            {
                PurchaseOrders.Add(PurchaseOrderListItemViewModel.FromResult(purchaseOrder));
            }

            if (CurrentPurchaseOrder is not null)
            {
                if (PurchaseOrders.Any(item => item.Id == CurrentPurchaseOrder.Id))
                {
                    SetCurrent(await _purchaseOrderService.GetByIdAsync(CurrentPurchaseOrder.Id));
                }
                else
                {
                    ClearCurrent();
                }
            }
        }
        catch (AccessDeniedException)
        {
            PurchaseOrders.Clear();
            ClearCurrent();
            ErrorMessage = "Você não tem permissão para consultar pedidos de compra.";
        }
        catch (Exception exception)
        {
            PurchaseOrders.Clear();
            ClearCurrent();
            ErrorMessage = "Não foi possível carregar os pedidos de compra. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load purchase orders workspace");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public async Task SelectAsync(Guid purchaseOrderId)
    {
        SetCurrent(await _purchaseOrderService.GetByIdAsync(purchaseOrderId));
    }

    public async Task RefreshCreationCandidatesAsync()
    {
        var candidates = await _creationLookupService.ListAvailableAsync();
        CreationCandidates.Clear();
        foreach (var candidate in candidates)
        {
            CreationCandidates.Add(PurchaseOrderCreationCandidateViewModel.FromResult(candidate));
        }
    }

    public async Task<PurchaseOrderResult> CreateDraftAsync(
        Guid requestForQuotationAwardId,
        Guid supplierId)
    {
        var result = await _purchaseOrderService.CreateDraftFromAwardAsync(
            requestForQuotationAwardId,
            supplierId);
        ApplyResult(result);
        await RefreshCreationCandidatesAsync();
        return result;
    }

    public async Task<PurchaseOrderResult> UpdateNotesAsync(string? notes)
    {
        var current = RequireCurrent();
        var result = await _purchaseOrderService.UpdateDraftAsync(current.Id, notes);
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseOrderResult> IssueAsync()
    {
        var current = RequireCurrent();
        var result = await _purchaseOrderService.IssueAsync(current.Id);
        ApplyResult(result);
        return result;
    }

    public async Task<PurchaseOrderResult> CancelAsync(string reason)
    {
        var current = RequireCurrent();
        var result = await _purchaseOrderService.CancelAsync(current.Id, reason);
        ApplyResult(result);
        return result;
    }

    public void ClearCurrent()
    {
        CurrentPurchaseOrder = null;
        CurrentLines.Clear();
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    partial void OnCurrentPurchaseOrderChanged(PurchaseOrderResult? value) => NotifyDerivedState();

    private PurchaseOrderResult RequireCurrent()
    {
        return CurrentPurchaseOrder
            ?? throw new InvalidOperationException("No purchase order is selected.");
    }

    private void ApplyResult(PurchaseOrderResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var existing = PurchaseOrders.FirstOrDefault(item => item.Id == result.Id);
        if (existing is not null)
        {
            PurchaseOrders.Remove(existing);
        }

        if (MatchesCurrentFilter(result))
        {
            var item = PurchaseOrderListItemViewModel.FromResult(result);
            var insertIndex = 0;
            while (insertIndex < PurchaseOrders.Count && PurchaseOrders[insertIndex].SequenceNumber > item.SequenceNumber)
            {
                insertIndex++;
            }

            PurchaseOrders.Insert(insertIndex, item);
        }

        SetCurrent(result);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    private void SetCurrent(PurchaseOrderResult result)
    {
        CurrentPurchaseOrder = result ?? throw new ArgumentNullException(nameof(result));
        CurrentLines.Clear();
        foreach (var line in result.Lines.OrderBy(item => item.LineNumber))
        {
            CurrentLines.Add(PurchaseOrderLineListItemViewModel.FromResult(line, result.CurrencyCode));
        }

        NotifyDerivedState();
    }

    private bool MatchesCurrentFilter(PurchaseOrderResult result)
    {
        return SelectedStatusFilter.Status is null || SelectedStatusFilter.Status == result.Status;
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasPurchaseOrders));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasCurrentPurchaseOrder));
        OnPropertyChanged(nameof(IsDraft));
        OnPropertyChanged(nameof(IsIssued));
        OnPropertyChanged(nameof(HasIssuedHistory));
        OnPropertyChanged(nameof(IsCancelled));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(CanIssue));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CurrentNumberDisplay));
        OnPropertyChanged(nameof(CurrentStatusLabel));
        OnPropertyChanged(nameof(CurrentCreatedDisplay));
        OnPropertyChanged(nameof(CurrentSupplierDisplay));
        OnPropertyChanged(nameof(CurrentCurrencyDisplay));
        OnPropertyChanged(nameof(CurrentTotalDisplay));
        OnPropertyChanged(nameof(CurrentAwardIdDisplay));
        OnPropertyChanged(nameof(CurrentIssuedDisplay));
        OnPropertyChanged(nameof(CurrentCancelledDisplay));
        OnPropertyChanged(nameof(CurrentCancellationReasonDisplay));
    }

    public static string FormatNumber(int sequenceNumber) => $"PC-{sequenceNumber:000000}";

    public static string StatusLabel(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "Rascunho",
        PurchaseOrderStatus.Issued => "Emitido",
        PurchaseOrderStatus.Cancelled => "Cancelado",
        _ => status.ToString(),
    };

    public static string FormatMoney(decimal amount, string currencyCode) =>
        $"{currencyCode} {amount.ToString("N2", CultureInfo.CurrentCulture)}";
}

public sealed record PurchaseOrderStatusFilterOption(string Label, PurchaseOrderStatus? Status);

public sealed record PurchaseOrderListItemViewModel(
    Guid Id,
    int SequenceNumber,
    string SupplierName,
    string CurrencyCode,
    decimal Total,
    PurchaseOrderStatus Status,
    DateTime CreatedAtUtc)
{
    public string NumberDisplay => PurchaseOrdersWorkspaceViewModel.FormatNumber(SequenceNumber);

    public string StatusLabel => PurchaseOrdersWorkspaceViewModel.StatusLabel(Status);

    public string TotalDisplay => PurchaseOrdersWorkspaceViewModel.FormatMoney(Total, CurrencyCode);

    public string CreatedDisplay => CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

    public static PurchaseOrderListItemViewModel FromResult(PurchaseOrderResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PurchaseOrderListItemViewModel(
            result.Id,
            result.SequenceNumber,
            result.SupplierNameSnapshot,
            result.CurrencyCode,
            result.Total,
            result.Status,
            result.CreatedAtUtc);
    }
}

public sealed record PurchaseOrderLineListItemViewModel(
    int LineNumber,
    string Description,
    decimal Quantity,
    string MeasurementUnitName,
    decimal UnitPrice,
    decimal LineTotal,
    DateOnly? PromisedDeliveryDate,
    string CurrencyCode)
{
    public string QuantityDisplay => Quantity.ToString("0.####", CultureInfo.CurrentCulture);

    public string UnitPriceDisplay => $"{CurrencyCode} {UnitPrice.ToString("0.######", CultureInfo.CurrentCulture)}";

    public string LineTotalDisplay => $"{CurrencyCode} {LineTotal.ToString("0.######", CultureInfo.CurrentCulture)}";

    public string PromisedDeliveryDateDisplay => PromisedDeliveryDate?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";

    public static PurchaseOrderLineListItemViewModel FromResult(
        PurchaseOrderLineResult result,
        string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PurchaseOrderLineListItemViewModel(
            result.LineNumber,
            result.DescriptionSnapshot,
            result.Quantity,
            result.MeasurementUnitNameSnapshot,
            result.UnitPrice,
            result.LineTotal,
            result.PromisedDeliveryDate,
            currencyCode);
    }
}

public sealed record PurchaseOrderCreationCandidateViewModel(
    Guid RequestForQuotationAwardId,
    Guid RequestForQuotationId,
    DateTime AwardFinalizedAtUtc,
    Guid SupplierId,
    string SupplierName,
    int AwardedLineCount)
{
    public string AwardFinalizedDisplay => AwardFinalizedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

    public string RequestForQuotationDisplay => $"RFQ {RequestForQuotationId.ToString("N")[..8].ToUpperInvariant()}";

    public string AwardedLinesDisplay => $"{AwardedLineCount} linha(s) adjudicada(s)";

    public static PurchaseOrderCreationCandidateViewModel FromResult(PurchaseOrderCreationCandidateResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PurchaseOrderCreationCandidateViewModel(
            result.RequestForQuotationAwardId,
            result.RequestForQuotationId,
            result.AwardFinalizedAtUtc,
            result.SupplierId,
            result.SupplierName,
            result.AwardedLineCount);
    }
}
