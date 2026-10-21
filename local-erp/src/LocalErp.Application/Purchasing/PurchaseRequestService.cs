using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Purchasing;

public sealed class PurchaseRequestService(
    IPurchaseRequestRepository repository,
    IMeasurementUnitRepository measurementUnitRepository,
    IAuthorizationService authorizationService,
    IUserSession session,
    IClock clock)
{
    private readonly IPurchaseRequestRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IMeasurementUnitRepository _measurementUnitRepository = measurementUnitRepository
        ?? throw new ArgumentNullException(nameof(measurementUnitRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    public async Task<PurchaseRequestResult> CreateDraftAsync(
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsManage, cancellationToken);
        var actorUserId = GetCurrentUserId();
        var purchaseRequest = new PurchaseRequest(actorUserId, _clock.UtcNow, notes);
        await _repository.AddAsync(purchaseRequest, cancellationToken);
        return PurchaseRequestResult.FromDomain(purchaseRequest);
    }

    public async Task<IReadOnlyList<PurchaseRequestResult>> ListAsync(
        PurchaseRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsRead, cancellationToken);
        var purchaseRequests = await _repository.ListAsync(status, cancellationToken);
        return purchaseRequests.Select(PurchaseRequestResult.FromDomain).ToArray();
    }

    public async Task<PurchaseRequestResult> GetByIdAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsRead, cancellationToken);
        return PurchaseRequestResult.FromDomain(
            await GetRequiredAsync(purchaseRequestId, cancellationToken));
    }

    public async Task<PurchaseRequestResult> UpdateDraftAsync(
        Guid purchaseRequestId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsManage, cancellationToken);
        var purchaseRequest = await GetRequiredAsync(purchaseRequestId, cancellationToken);
        purchaseRequest.UpdateNotes(notes);
        await _repository.UpdateAsync(purchaseRequest, cancellationToken);
        return PurchaseRequestResult.FromDomain(purchaseRequest);
    }

    public async Task<PurchaseRequestResult> AddLineAsync(
        Guid purchaseRequestId,
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsManage, cancellationToken);
        await ValidateActiveMeasurementUnitAsync(measurementUnitId, cancellationToken);
        var purchaseRequest = await GetRequiredAsync(purchaseRequestId, cancellationToken);
        purchaseRequest.AddLine(description, quantity, measurementUnitId, neededByDate);
        await _repository.UpdateAsync(purchaseRequest, cancellationToken);
        return PurchaseRequestResult.FromDomain(purchaseRequest);
    }

    public async Task<PurchaseRequestResult> UpdateLineAsync(
        Guid purchaseRequestId,
        Guid lineId,
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsManage, cancellationToken);
        await ValidateActiveMeasurementUnitAsync(measurementUnitId, cancellationToken);
        var purchaseRequest = await GetRequiredAsync(purchaseRequestId, cancellationToken);
        purchaseRequest.UpdateLine(lineId, description, quantity, measurementUnitId, neededByDate);
        await _repository.UpdateAsync(purchaseRequest, cancellationToken);
        return PurchaseRequestResult.FromDomain(purchaseRequest);
    }

    public async Task<PurchaseRequestResult> RemoveLineAsync(
        Guid purchaseRequestId,
        Guid lineId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsManage, cancellationToken);
        var purchaseRequest = await GetRequiredAsync(purchaseRequestId, cancellationToken);
        purchaseRequest.RemoveLine(lineId);
        await _repository.UpdateAsync(purchaseRequest, cancellationToken);
        return PurchaseRequestResult.FromDomain(purchaseRequest);
    }

    public async Task<PurchaseRequestResult> SubmitAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsManage, cancellationToken);
        var actorUserId = GetCurrentUserId();
        var purchaseRequest = await GetRequiredAsync(purchaseRequestId, cancellationToken);
        purchaseRequest.Submit(actorUserId, _clock.UtcNow);
        await _repository.UpdateAsync(purchaseRequest, cancellationToken);
        return PurchaseRequestResult.FromDomain(purchaseRequest);
    }

    public async Task<PurchaseRequestResult> CancelAsync(
        Guid purchaseRequestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseRequestsManage, cancellationToken);
        var actorUserId = GetCurrentUserId();
        var purchaseRequest = await GetRequiredAsync(purchaseRequestId, cancellationToken);
        purchaseRequest.Cancel(actorUserId, _clock.UtcNow, reason);
        await _repository.UpdateAsync(purchaseRequest, cancellationToken);
        return PurchaseRequestResult.FromDomain(purchaseRequest);
    }

    private async Task<PurchaseRequest> GetRequiredAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken)
    {
        if (purchaseRequestId == Guid.Empty)
        {
            throw new ArgumentException("Purchase request identifier cannot be empty.", nameof(purchaseRequestId));
        }

        return await _repository.GetByIdAsync(purchaseRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase request '{purchaseRequestId}' was not found.");
    }

    private async Task ValidateActiveMeasurementUnitAsync(
        Guid measurementUnitId,
        CancellationToken cancellationToken)
    {
        if (measurementUnitId == Guid.Empty)
        {
            throw new ArgumentException("Measurement unit identifier cannot be empty.", nameof(measurementUnitId));
        }

        var unit = await _measurementUnitRepository.GetByIdAsync(measurementUnitId, cancellationToken)
            ?? throw new KeyNotFoundException($"Measurement unit '{measurementUnitId}' was not found.");
        if (!unit.IsActive)
        {
            throw new InvalidOperationException("Inactive measurement units cannot be used in purchase request lines.");
        }
    }

    private Guid GetCurrentUserId()
    {
        var currentUser = _session.CurrentUser
            ?? throw new InvalidOperationException("Purchase request mutations require an authenticated session.");
        if (!currentUser.IsActive)
        {
            throw new InvalidOperationException("The authenticated user is not active.");
        }

        return currentUser.Id;
    }
}
