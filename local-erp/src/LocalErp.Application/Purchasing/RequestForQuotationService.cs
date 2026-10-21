using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Purchasing;

public sealed class RequestForQuotationService(
    IRequestForQuotationRepository repository,
    IPurchaseRequestRepository purchaseRequestRepository,
    ISupplierRepository supplierRepository,
    IAuthorizationService authorizationService,
    IUserSession session,
    IClock clock)
{
    private readonly IRequestForQuotationRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPurchaseRequestRepository _purchaseRequestRepository = purchaseRequestRepository
        ?? throw new ArgumentNullException(nameof(purchaseRequestRepository));
    private readonly ISupplierRepository _supplierRepository = supplierRepository
        ?? throw new ArgumentNullException(nameof(supplierRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    public async Task<RequestForQuotationResult> CreateDraftAsync(
        Guid purchaseRequestId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var purchaseRequest = await GetRequiredPurchaseRequestAsync(purchaseRequestId, cancellationToken);
        if (purchaseRequest.Status != PurchaseRequestStatus.Submitted)
        {
            throw new InvalidOperationException("RFQs can only be created from submitted purchase requests.");
        }

        var requestForQuotation = new RequestForQuotation(
            purchaseRequest.Id,
            GetCurrentUserId(),
            _clock.UtcNow,
            notes);
        await _repository.AddAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<IReadOnlyList<RequestForQuotationResult>> ListAsync(
        RequestForQuotationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsRead, cancellationToken);
        var items = await _repository.ListAsync(status, cancellationToken);
        return items.Select(RequestForQuotationResult.FromDomain).ToArray();
    }

    public async Task<RequestForQuotationResult> GetByIdAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsRead, cancellationToken);
        return RequestForQuotationResult.FromDomain(await GetRequiredAsync(requestForQuotationId, cancellationToken));
    }

    public async Task<RequestForQuotationResult> UpdateDraftAsync(
        Guid requestForQuotationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.UpdateNotes(notes);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> AddSourceLineAsync(
        Guid requestForQuotationId,
        Guid purchaseRequestLineId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        var purchaseRequest = await GetRequiredPurchaseRequestAsync(requestForQuotation.PurchaseRequestId, cancellationToken);
        if (!purchaseRequest.Lines.Any(line => line.Id == purchaseRequestLineId))
        {
            throw new KeyNotFoundException($"Purchase request line '{purchaseRequestLineId}' does not belong to the RFQ source request.");
        }

        requestForQuotation.AddSourceLine(purchaseRequestLineId);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> RemoveSourceLineAsync(
        Guid requestForQuotationId,
        Guid requestForQuotationLineId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.RemoveSourceLine(requestForQuotationLineId);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> AddSupplierAsync(
        Guid requestForQuotationId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        await ValidateActiveSupplierAsync(supplierId, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.AddSupplier(supplierId);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> RemoveSupplierAsync(
        Guid requestForQuotationId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.RemoveSupplier(supplierId);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> SendAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.Send(GetCurrentUserId(), _clock.UtcNow);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> CreateSupplierQuotationAsync(
        Guid requestForQuotationId,
        Guid supplierId,
        string currencyCode,
        string? supplierReference = null,
        DateOnly? validUntilDate = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.CreateSupplierQuotation(
            supplierId,
            currencyCode,
            GetCurrentUserId(),
            _clock.UtcNow,
            supplierReference,
            validUntilDate,
            notes);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> UpdateSupplierQuotationAsync(
        Guid requestForQuotationId,
        Guid supplierQuotationId,
        string currencyCode,
        string? supplierReference = null,
        DateOnly? validUntilDate = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.UpdateSupplierQuotation(
            supplierQuotationId,
            currencyCode,
            supplierReference,
            validUntilDate,
            notes);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> UpsertSupplierQuotationLineAsync(
        Guid requestForQuotationId,
        Guid supplierQuotationId,
        Guid requestForQuotationLineId,
        decimal unitPrice,
        DateOnly? promisedDeliveryDate = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.UpsertSupplierQuotationLine(
            supplierQuotationId,
            requestForQuotationLineId,
            unitPrice,
            promisedDeliveryDate,
            notes);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> RemoveSupplierQuotationLineAsync(
        Guid requestForQuotationId,
        Guid supplierQuotationId,
        Guid supplierQuotationLineId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.RemoveSupplierQuotationLine(supplierQuotationId, supplierQuotationLineId);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> FinalizeSupplierQuotationAsync(
        Guid requestForQuotationId,
        Guid supplierQuotationId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.FinalizeSupplierQuotation(supplierQuotationId, GetCurrentUserId(), _clock.UtcNow);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> CloseAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.Close(GetCurrentUserId(), _clock.UtcNow);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    public async Task<RequestForQuotationResult> CancelAsync(
        Guid requestForQuotationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requestForQuotation = await GetRequiredAsync(requestForQuotationId, cancellationToken);
        requestForQuotation.Cancel(GetCurrentUserId(), _clock.UtcNow, reason);
        await _repository.UpdateAsync(requestForQuotation, cancellationToken);
        return RequestForQuotationResult.FromDomain(requestForQuotation);
    }

    private async Task<RequestForQuotation> GetRequiredAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken)
    {
        if (requestForQuotationId == Guid.Empty)
        {
            throw new ArgumentException("RFQ identifier cannot be empty.", nameof(requestForQuotationId));
        }

        return await _repository.GetByIdAsync(requestForQuotationId, cancellationToken)
            ?? throw new KeyNotFoundException($"RFQ '{requestForQuotationId}' was not found.");
    }

    private async Task<PurchaseRequest> GetRequiredPurchaseRequestAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken)
    {
        if (purchaseRequestId == Guid.Empty)
        {
            throw new ArgumentException("Purchase request identifier cannot be empty.", nameof(purchaseRequestId));
        }

        return await _purchaseRequestRepository.GetByIdAsync(purchaseRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase request '{purchaseRequestId}' was not found.");
    }

    private async Task ValidateActiveSupplierAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        if (supplierId == Guid.Empty)
        {
            throw new ArgumentException("Supplier identifier cannot be empty.", nameof(supplierId));
        }

        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier '{supplierId}' was not found.");
        if (!supplier.IsActive)
        {
            throw new InvalidOperationException("Inactive suppliers cannot be invited to an RFQ.");
        }
    }

    private Guid GetCurrentUserId()
    {
        var currentUser = _session.CurrentUser
            ?? throw new InvalidOperationException("RFQ mutations require an authenticated session.");
        if (!currentUser.IsActive)
        {
            throw new InvalidOperationException("The authenticated user is not active.");
        }

        return currentUser.Id;
    }
}
