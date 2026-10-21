using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using Serilog;

namespace LocalErp.Desktop.ViewModels;

public partial class RequestForQuotationsWorkspaceViewModel : ObservableObject
{
    private readonly RequestForQuotationService _service;
    private readonly RequestForQuotationLookupService _lookupService;
    private IReadOnlyDictionary<Guid, RequestForQuotationSupplierOption> _supplierReferences =
        new Dictionary<Guid, RequestForQuotationSupplierOption>();

    public RequestForQuotationsWorkspaceViewModel(
        RequestForQuotationService service,
        RequestForQuotationLookupService lookupService)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
        StatusFilters =
        [
            new("Todos", null),
            new("Rascunho", RequestForQuotationStatus.Draft),
            new("Enviada", RequestForQuotationStatus.Sent),
            new("Encerrada", RequestForQuotationStatus.Closed),
            new("Cancelada", RequestForQuotationStatus.Cancelled),
        ];
        selectedStatusFilter = StatusFilters[0];
    }

    public ObservableCollection<RequestForQuotationListItemViewModel> RequestsForQuotation { get; } = [];

    public ObservableCollection<RequestForQuotationSourceRequestListItemViewModel> CreationSources { get; } = [];

    public ObservableCollection<RequestForQuotationSourceLineListItemViewModel> CurrentSourceLines { get; } = [];

    public ObservableCollection<RequestForQuotationSourceLineListItemViewModel> AvailableSourceLines { get; } = [];

    public ObservableCollection<RequestForQuotationSupplierOption> ActiveSuppliers { get; } = [];

    public ObservableCollection<RequestForQuotationSupplierOption> AvailableSuppliers { get; } = [];

    public ObservableCollection<RequestForQuotationSupplierListItemViewModel> CurrentSuppliers { get; } = [];

    public ObservableCollection<RequestForQuotationSupplierQuotationListItemViewModel> CurrentSupplierQuotations { get; } = [];

    public ObservableCollection<RequestForQuotationSupplierQuotationLineListItemViewModel> CurrentQuotationLines { get; } = [];

    public IReadOnlyList<RequestForQuotationStatusFilterOption> StatusFilters { get; }

    [ObservableProperty]
    private RequestForQuotationStatusFilterOption selectedStatusFilter;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasLoaded;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private RequestForQuotationResult? currentRequestForQuotation;

    [ObservableProperty]
    private RequestForQuotationPurchaseRequestOption? currentSourceRequest;

    [ObservableProperty]
    private SupplierQuotationResult? currentSupplierQuotation;

    public bool HasRequestsForQuotation => RequestsForQuotation.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmpty => HasLoaded && !IsLoading && !HasError && RequestsForQuotation.Count == 0;

    public bool HasCurrentRequestForQuotation => CurrentRequestForQuotation is not null;

    public bool IsDraft => CurrentRequestForQuotation?.Status == RequestForQuotationStatus.Draft;

    public bool IsSent => CurrentRequestForQuotation?.Status == RequestForQuotationStatus.Sent;

    public bool IsClosed => CurrentRequestForQuotation?.Status == RequestForQuotationStatus.Closed;

    public bool IsCancelled => CurrentRequestForQuotation?.Status == RequestForQuotationStatus.Cancelled;

    public bool IsStructureReadOnly => CurrentRequestForQuotation is not null && !IsDraft;

    public bool CanCancel => CurrentRequestForQuotation?.Status is RequestForQuotationStatus.Draft or RequestForQuotationStatus.Sent;

    public bool CanClose => IsSent;

    public bool HasCurrentSupplierQuotation => CurrentSupplierQuotation is not null;

    public bool CanEditCurrentSupplierQuotation => IsSent && CurrentSupplierQuotation?.Status == SupplierQuotationStatus.Draft;

    public bool IsCurrentSupplierQuotationFinalized => CurrentSupplierQuotation?.Status == SupplierQuotationStatus.Finalized;

    public string CurrentStatusLabel => CurrentRequestForQuotation is null
        ? string.Empty
        : StatusLabel(CurrentRequestForQuotation.Status);

    public string CurrentCreatedDisplay => CurrentRequestForQuotation is null
        ? string.Empty
        : CurrentRequestForQuotation.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

    public string CurrentIdDisplay => CurrentRequestForQuotation?.Id.ToString("D") ?? string.Empty;

    public string CurrentSourceRequestDisplay => CurrentSourceRequest is null
        ? string.Empty
        : $"{CurrentSourceRequest.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)} · {CurrentSourceRequest.Lines.Count} linha(s)";

    public string CurrentSupplierQuotationStatusLabel => CurrentSupplierQuotation is null
        ? string.Empty
        : SupplierQuotationStatusLabel(CurrentSupplierQuotation.Status);

    public string CurrentSupplierQuotationSupplierName => CurrentSupplierQuotation is null
        ? string.Empty
        : SupplierName(CurrentSupplierQuotation.SupplierId);

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
            var items = await _service.ListAsync(SelectedStatusFilter.Status);
            RequestsForQuotation.Clear();
            foreach (var item in items
                         .OrderByDescending(request => request.CreatedAtUtc)
                         .ThenBy(request => request.Id))
            {
                RequestsForQuotation.Add(RequestForQuotationListItemViewModel.FromResult(item));
            }

            if (CurrentRequestForQuotation is not null)
            {
                if (RequestsForQuotation.Any(item => item.Id == CurrentRequestForQuotation.Id))
                {
                    var refreshed = await _service.GetByIdAsync(CurrentRequestForQuotation.Id);
                    await SetCurrentAsync(refreshed, CurrentSupplierQuotation?.Id);
                }
                else
                {
                    ClearCurrent();
                }
            }
        }
        catch (AccessDeniedException)
        {
            RequestsForQuotation.Clear();
            ClearCurrent();
            ErrorMessage = "Você não tem permissão para consultar cotações/RFQs.";
        }
        catch (Exception exception)
        {
            RequestsForQuotation.Clear();
            ClearCurrent();
            ErrorMessage = "Não foi possível carregar as cotações/RFQs. Consulte os logs para detalhes técnicos.";
            Log.Error(exception, "Failed to load RFQ workspace");
        }
        finally
        {
            HasLoaded = true;
            IsLoading = false;
            NotifyDerivedState();
        }
    }

    public async Task SelectAsync(Guid requestForQuotationId)
    {
        var result = await _service.GetByIdAsync(requestForQuotationId);
        await SetCurrentAsync(result, null);
    }

    public async Task EnsureCreationSourcesAsync()
    {
        var sources = await _lookupService.ListSubmittedPurchaseRequestsAsync();
        CreationSources.Clear();
        foreach (var source in sources)
        {
            CreationSources.Add(RequestForQuotationSourceRequestListItemViewModel.FromOption(source));
        }
    }

    public async Task EnsureActiveSuppliersAsync()
    {
        var suppliers = await _lookupService.ListActiveSuppliersAsync();
        ActiveSuppliers.Clear();
        foreach (var supplier in suppliers)
        {
            ActiveSuppliers.Add(supplier);
        }

        RebuildAvailableSuppliers();
    }

    public async Task<RequestForQuotationResult> CreateDraftAsync(Guid purchaseRequestId)
    {
        var result = await _service.CreateDraftAsync(purchaseRequestId);
        await ApplyResultAsync(result, null);
        return result;
    }

    public async Task<RequestForQuotationResult> UpdateNotesAsync(string? notes)
    {
        var current = RequireCurrent();
        var result = await _service.UpdateDraftAsync(current.Id, notes);
        await ApplyResultAsync(result, CurrentSupplierQuotation?.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> AddSourceLineAsync(Guid purchaseRequestLineId)
    {
        var current = RequireCurrent();
        var result = await _service.AddSourceLineAsync(current.Id, purchaseRequestLineId);
        await ApplyResultAsync(result, CurrentSupplierQuotation?.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> RemoveSourceLineAsync(Guid requestForQuotationLineId)
    {
        var current = RequireCurrent();
        var result = await _service.RemoveSourceLineAsync(current.Id, requestForQuotationLineId);
        await ApplyResultAsync(result, CurrentSupplierQuotation?.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> AddSupplierAsync(Guid supplierId)
    {
        var current = RequireCurrent();
        var result = await _service.AddSupplierAsync(current.Id, supplierId);
        await ApplyResultAsync(result, CurrentSupplierQuotation?.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> RemoveSupplierAsync(Guid supplierId)
    {
        var current = RequireCurrent();
        var result = await _service.RemoveSupplierAsync(current.Id, supplierId);
        await ApplyResultAsync(result, CurrentSupplierQuotation?.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> SendAsync()
    {
        var current = RequireCurrent();
        var result = await _service.SendAsync(current.Id);
        await ApplyResultAsync(result, null);
        return result;
    }

    public async Task<RequestForQuotationResult> CreateSupplierQuotationAsync(
        Guid supplierId,
        string currencyCode,
        string? supplierReference,
        DateOnly? validUntilDate,
        string? notes)
    {
        var current = RequireCurrent();
        var result = await _service.CreateSupplierQuotationAsync(
            current.Id,
            supplierId,
            currencyCode,
            supplierReference,
            validUntilDate,
            notes);
        var created = result.SupplierQuotations.Single(quotation => quotation.SupplierId == supplierId);
        await ApplyResultAsync(result, created.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> UpdateSupplierQuotationAsync(
        string currencyCode,
        string? supplierReference,
        DateOnly? validUntilDate,
        string? notes)
    {
        var current = RequireCurrent();
        var quotation = RequireCurrentSupplierQuotation();
        var result = await _service.UpdateSupplierQuotationAsync(
            current.Id,
            quotation.Id,
            currencyCode,
            supplierReference,
            validUntilDate,
            notes);
        await ApplyResultAsync(result, quotation.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> UpsertSupplierQuotationLineAsync(
        Guid requestForQuotationLineId,
        decimal unitPrice,
        DateOnly? promisedDeliveryDate,
        string? notes)
    {
        var current = RequireCurrent();
        var quotation = RequireCurrentSupplierQuotation();
        var result = await _service.UpsertSupplierQuotationLineAsync(
            current.Id,
            quotation.Id,
            requestForQuotationLineId,
            unitPrice,
            promisedDeliveryDate,
            notes);
        await ApplyResultAsync(result, quotation.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> RemoveSupplierQuotationLineAsync(Guid supplierQuotationLineId)
    {
        var current = RequireCurrent();
        var quotation = RequireCurrentSupplierQuotation();
        var result = await _service.RemoveSupplierQuotationLineAsync(
            current.Id,
            quotation.Id,
            supplierQuotationLineId);
        await ApplyResultAsync(result, quotation.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> FinalizeSupplierQuotationAsync()
    {
        var current = RequireCurrent();
        var quotation = RequireCurrentSupplierQuotation();
        var result = await _service.FinalizeSupplierQuotationAsync(current.Id, quotation.Id);
        await ApplyResultAsync(result, quotation.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> CloseAsync()
    {
        var current = RequireCurrent();
        var result = await _service.CloseAsync(current.Id);
        await ApplyResultAsync(result, CurrentSupplierQuotation?.Id);
        return result;
    }

    public async Task<RequestForQuotationResult> CancelAsync(string reason)
    {
        var current = RequireCurrent();
        var result = await _service.CancelAsync(current.Id, reason);
        await ApplyResultAsync(result, CurrentSupplierQuotation?.Id);
        return result;
    }

    public void SelectSupplierQuotation(Guid supplierQuotationId)
    {
        if (CurrentRequestForQuotation is null)
        {
            return;
        }

        CurrentSupplierQuotation = CurrentRequestForQuotation.SupplierQuotations
            .SingleOrDefault(quotation => quotation.Id == supplierQuotationId)
            ?? throw new KeyNotFoundException($"Supplier quotation '{supplierQuotationId}' was not found.");
        RebuildCurrentQuotationLines();
        NotifyDerivedState();
    }

    public void ClearCurrentSupplierQuotation()
    {
        CurrentSupplierQuotation = null;
        CurrentQuotationLines.Clear();
        NotifyDerivedState();
    }

    public void ClearCurrent()
    {
        CurrentRequestForQuotation = null;
        CurrentSourceRequest = null;
        CurrentSupplierQuotation = null;
        CurrentSourceLines.Clear();
        AvailableSourceLines.Clear();
        CurrentSuppliers.Clear();
        CurrentSupplierQuotations.Clear();
        CurrentQuotationLines.Clear();
        AvailableSuppliers.Clear();
        _supplierReferences = new Dictionary<Guid, RequestForQuotationSupplierOption>();
        NotifyDerivedState();
    }

    partial void OnIsLoadingChanged(bool value) => NotifyDerivedState();

    partial void OnHasLoadedChanged(bool value) => NotifyDerivedState();

    partial void OnErrorMessageChanged(string? value) => NotifyDerivedState();

    partial void OnCurrentRequestForQuotationChanged(RequestForQuotationResult? value) => NotifyDerivedState();

    partial void OnCurrentSupplierQuotationChanged(SupplierQuotationResult? value) => NotifyDerivedState();

    private RequestForQuotationResult RequireCurrent()
    {
        return CurrentRequestForQuotation ?? throw new InvalidOperationException("No RFQ is selected.");
    }

    private SupplierQuotationResult RequireCurrentSupplierQuotation()
    {
        return CurrentSupplierQuotation ?? throw new InvalidOperationException("No supplier quotation is selected.");
    }

    private async Task ApplyResultAsync(RequestForQuotationResult result, Guid? preferredQuotationId)
    {
        ArgumentNullException.ThrowIfNull(result);
        var existing = RequestsForQuotation.FirstOrDefault(item => item.Id == result.Id);
        if (existing is not null)
        {
            RequestsForQuotation.Remove(existing);
        }

        if (MatchesCurrentFilter(result))
        {
            var item = RequestForQuotationListItemViewModel.FromResult(result);
            var insertIndex = 0;
            while (insertIndex < RequestsForQuotation.Count && Compare(RequestsForQuotation[insertIndex], item) < 0)
            {
                insertIndex++;
            }

            RequestsForQuotation.Insert(insertIndex, item);
        }

        await SetCurrentAsync(result, preferredQuotationId);
        HasLoaded = true;
        ErrorMessage = null;
        NotifyDerivedState();
    }

    private async Task SetCurrentAsync(RequestForQuotationResult result, Guid? preferredQuotationId)
    {
        CurrentRequestForQuotation = result ?? throw new ArgumentNullException(nameof(result));
        CurrentSourceRequest = await _lookupService.GetPurchaseRequestAsync(result.PurchaseRequestId);

        var supplierIds = result.Suppliers.Select(item => item.SupplierId)
            .Concat(result.SupplierQuotations.Select(item => item.SupplierId))
            .Distinct()
            .ToArray();
        var suppliers = supplierIds.Length == 0
            ? Array.Empty<RequestForQuotationSupplierOption>()
            : await _lookupService.GetSupplierReferencesAsync(supplierIds);
        _supplierReferences = suppliers.ToDictionary(item => item.Id);

        RebuildSourceLines();
        RebuildSuppliers();
        RebuildSupplierQuotations();
        RebuildAvailableSuppliers();

        CurrentSupplierQuotation = preferredQuotationId is Guid quotationId
            ? result.SupplierQuotations.SingleOrDefault(quotation => quotation.Id == quotationId)
            : null;
        RebuildCurrentQuotationLines();
        NotifyDerivedState();
    }

    private void RebuildSourceLines()
    {
        CurrentSourceLines.Clear();
        AvailableSourceLines.Clear();
        if (CurrentRequestForQuotation is null || CurrentSourceRequest is null)
        {
            return;
        }

        var includedBySourceId = CurrentRequestForQuotation.Lines.ToDictionary(line => line.PurchaseRequestLineId);
        foreach (var sourceLine in CurrentSourceRequest.Lines.OrderBy(line => line.LineNumber))
        {
            if (includedBySourceId.TryGetValue(sourceLine.Id, out var rfqLine))
            {
                CurrentSourceLines.Add(RequestForQuotationSourceLineListItemViewModel.FromIncluded(rfqLine, sourceLine));
            }
            else
            {
                AvailableSourceLines.Add(RequestForQuotationSourceLineListItemViewModel.FromAvailable(sourceLine));
            }
        }
    }

    private void RebuildSuppliers()
    {
        CurrentSuppliers.Clear();
        if (CurrentRequestForQuotation is null)
        {
            return;
        }

        foreach (var supplier in CurrentRequestForQuotation.Suppliers)
        {
            var quotation = CurrentRequestForQuotation.SupplierQuotations
                .SingleOrDefault(item => item.SupplierId == supplier.SupplierId);
            CurrentSuppliers.Add(new RequestForQuotationSupplierListItemViewModel(
                supplier.SupplierId,
                SupplierName(supplier.SupplierId),
                _supplierReferences.TryGetValue(supplier.SupplierId, out var reference) && reference.IsActive,
                quotation?.Id,
                quotation is null ? "Sem resposta" : SupplierQuotationStatusLabel(quotation.Status),
                IsSent && quotation is null,
                quotation is not null));
        }
    }

    private void RebuildAvailableSuppliers()
    {
        AvailableSuppliers.Clear();
        var invited = CurrentRequestForQuotation?.Suppliers.Select(item => item.SupplierId).ToHashSet() ?? [];
        foreach (var supplier in ActiveSuppliers.Where(item => !invited.Contains(item.Id)))
        {
            AvailableSuppliers.Add(supplier);
        }
    }

    private void RebuildSupplierQuotations()
    {
        CurrentSupplierQuotations.Clear();
        if (CurrentRequestForQuotation is null)
        {
            return;
        }

        foreach (var quotation in CurrentRequestForQuotation.SupplierQuotations)
        {
            CurrentSupplierQuotations.Add(RequestForQuotationSupplierQuotationListItemViewModel.FromResult(
                quotation,
                SupplierName(quotation.SupplierId)));
        }
    }

    private void RebuildCurrentQuotationLines()
    {
        CurrentQuotationLines.Clear();
        if (CurrentSupplierQuotation is null)
        {
            return;
        }

        foreach (var line in CurrentSupplierQuotation.Lines)
        {
            var source = CurrentSourceLines.SingleOrDefault(item => item.RequestForQuotationLineId == line.RequestForQuotationLineId);
            CurrentQuotationLines.Add(RequestForQuotationSupplierQuotationLineListItemViewModel.FromResult(line, source));
        }
    }

    private string SupplierName(Guid supplierId)
    {
        return _supplierReferences.TryGetValue(supplierId, out var supplier)
            ? supplier.Name
            : "Fornecedor não encontrado";
    }

    private bool MatchesCurrentFilter(RequestForQuotationResult result)
    {
        return SelectedStatusFilter.Status is null || SelectedStatusFilter.Status == result.Status;
    }

    private static int Compare(RequestForQuotationListItemViewModel left, RequestForQuotationListItemViewModel right)
    {
        var byDate = right.CreatedAtUtc.CompareTo(left.CreatedAtUtc);
        return byDate != 0 ? byDate : left.Id.CompareTo(right.Id);
    }

    private void NotifyDerivedState()
    {
        OnPropertyChanged(nameof(HasRequestsForQuotation));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasCurrentRequestForQuotation));
        OnPropertyChanged(nameof(IsDraft));
        OnPropertyChanged(nameof(IsSent));
        OnPropertyChanged(nameof(IsClosed));
        OnPropertyChanged(nameof(IsCancelled));
        OnPropertyChanged(nameof(IsStructureReadOnly));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanClose));
        OnPropertyChanged(nameof(HasCurrentSupplierQuotation));
        OnPropertyChanged(nameof(CanEditCurrentSupplierQuotation));
        OnPropertyChanged(nameof(IsCurrentSupplierQuotationFinalized));
        OnPropertyChanged(nameof(CurrentStatusLabel));
        OnPropertyChanged(nameof(CurrentCreatedDisplay));
        OnPropertyChanged(nameof(CurrentIdDisplay));
        OnPropertyChanged(nameof(CurrentSourceRequestDisplay));
        OnPropertyChanged(nameof(CurrentSupplierQuotationStatusLabel));
        OnPropertyChanged(nameof(CurrentSupplierQuotationSupplierName));
    }

    public static string StatusLabel(RequestForQuotationStatus status) => status switch
    {
        RequestForQuotationStatus.Draft => "Rascunho",
        RequestForQuotationStatus.Sent => "Enviada",
        RequestForQuotationStatus.Closed => "Encerrada",
        RequestForQuotationStatus.Cancelled => "Cancelada",
        _ => status.ToString(),
    };

    public static string SupplierQuotationStatusLabel(SupplierQuotationStatus status) => status switch
    {
        SupplierQuotationStatus.Draft => "Rascunho",
        SupplierQuotationStatus.Finalized => "Finalizada",
        _ => status.ToString(),
    };
}

public sealed record RequestForQuotationStatusFilterOption(string Label, RequestForQuotationStatus? Status);

public sealed record RequestForQuotationListItemViewModel(
    Guid Id,
    RequestForQuotationStatus Status,
    DateTime CreatedAtUtc,
    int LineCount,
    int SupplierCount,
    int ResponseCount,
    string? Notes)
{
    public string StatusLabel => RequestForQuotationsWorkspaceViewModel.StatusLabel(Status);
    public string CreatedDisplay => CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "—" : Notes;

    public static RequestForQuotationListItemViewModel FromResult(RequestForQuotationResult item) => new(
        item.Id,
        item.Status,
        item.CreatedAtUtc,
        item.Lines.Count,
        item.Suppliers.Count,
        item.SupplierQuotations.Count,
        item.Notes);
}

public sealed record RequestForQuotationSourceRequestListItemViewModel(
    Guid Id,
    DateTime CreatedAtUtc,
    string? Notes,
    int LineCount)
{
    public string Display => $"{CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture)} · {LineCount} linha(s) · {(string.IsNullOrWhiteSpace(Notes) ? "Sem observação" : Notes)}";

    public static RequestForQuotationSourceRequestListItemViewModel FromOption(RequestForQuotationPurchaseRequestOption option) => new(
        option.Id,
        option.CreatedAtUtc,
        option.Notes,
        option.Lines.Count);
}

public sealed record RequestForQuotationSourceLineListItemViewModel(
    Guid? RequestForQuotationLineId,
    Guid PurchaseRequestLineId,
    int LineNumber,
    string Description,
    decimal Quantity,
    string MeasurementUnitName,
    DateOnly? NeededByDate)
{
    public string QuantityDisplay => Quantity.ToString("0.####", CultureInfo.CurrentCulture);
    public string NeededByDateDisplay => NeededByDate?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";
    public string SelectionDisplay => $"#{LineNumber} · {Description} · {QuantityDisplay} {MeasurementUnitName}";

    public static RequestForQuotationSourceLineListItemViewModel FromIncluded(
        RequestForQuotationLineResult rfqLine,
        RequestForQuotationPurchaseRequestLineOption sourceLine) => new(
            rfqLine.Id,
            sourceLine.Id,
            rfqLine.LineNumber,
            sourceLine.Description,
            sourceLine.Quantity,
            sourceLine.MeasurementUnitName,
            sourceLine.NeededByDate);

    public static RequestForQuotationSourceLineListItemViewModel FromAvailable(
        RequestForQuotationPurchaseRequestLineOption sourceLine) => new(
            null,
            sourceLine.Id,
            sourceLine.LineNumber,
            sourceLine.Description,
            sourceLine.Quantity,
            sourceLine.MeasurementUnitName,
            sourceLine.NeededByDate);
}

public sealed record RequestForQuotationSupplierListItemViewModel(
    Guid SupplierId,
    string Name,
    bool IsActive,
    Guid? SupplierQuotationId,
    string ResponseStatusLabel,
    bool CanCreateResponse,
    bool HasResponse)
{
    public string ActiveLabel => IsActive ? "Ativo" : "Inativo";
}

public sealed record RequestForQuotationSupplierQuotationListItemViewModel(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    SupplierQuotationStatus Status,
    string CurrencyCode,
    int LineCount)
{
    public string StatusLabel => RequestForQuotationsWorkspaceViewModel.SupplierQuotationStatusLabel(Status);

    public static RequestForQuotationSupplierQuotationListItemViewModel FromResult(
        SupplierQuotationResult result,
        string supplierName) => new(
            result.Id,
            result.SupplierId,
            supplierName,
            result.Status,
            result.CurrencyCode,
            result.Lines.Count);
}

public sealed record RequestForQuotationSupplierQuotationLineListItemViewModel(
    Guid Id,
    Guid RequestForQuotationLineId,
    int SourceLineNumber,
    string Description,
    decimal UnitPrice,
    DateOnly? PromisedDeliveryDate,
    string? Notes)
{
    public string UnitPriceDisplay => UnitPrice.ToString("0.######", CultureInfo.CurrentCulture);
    public string PromisedDeliveryDateDisplay => PromisedDeliveryDate?.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture) ?? "—";
    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "—" : Notes;

    public static RequestForQuotationSupplierQuotationLineListItemViewModel FromResult(
        SupplierQuotationLineResult result,
        RequestForQuotationSourceLineListItemViewModel? source) => new(
            result.Id,
            result.RequestForQuotationLineId,
            source?.LineNumber ?? 0,
            source?.Description ?? "Linha não encontrada",
            result.UnitPrice,
            result.PromisedDeliveryDate,
            result.Notes);
}
