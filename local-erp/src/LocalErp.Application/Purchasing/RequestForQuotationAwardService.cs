using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Purchasing;

public sealed class RequestForQuotationAwardService(
    IRequestForQuotationAwardRepository repository,
    IRequestForQuotationRepository requestForQuotationRepository,
    IAuthorizationService authorizationService,
    IUserSession session,
    IClock clock)
{
    private readonly IRequestForQuotationAwardRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IRequestForQuotationRepository _requestForQuotationRepository = requestForQuotationRepository
        ?? throw new ArgumentNullException(nameof(requestForQuotationRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    public async Task<RequestForQuotationAwardResult> CreateDraftAsync(
        Guid requestForQuotationId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsAward, cancellationToken);
        var requestForQuotation = await GetRequiredRequestForQuotationAsync(requestForQuotationId, cancellationToken);
        EnsureClosed(requestForQuotation);

        if (await _repository.GetByRequestForQuotationIdAsync(requestForQuotation.Id, cancellationToken) is not null)
        {
            throw new InvalidOperationException("This RFQ already has an award decision.");
        }

        var award = new RequestForQuotationAward(
            requestForQuotation.Id,
            GetCurrentUserId(),
            _clock.UtcNow,
            notes);
        await _repository.AddAsync(award, cancellationToken);
        return RequestForQuotationAwardResult.FromDomain(award);
    }

    public async Task<RequestForQuotationAwardResult> GetByIdAsync(
        Guid awardId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsRead, cancellationToken);
        return RequestForQuotationAwardResult.FromDomain(await GetRequiredAwardAsync(awardId, cancellationToken));
    }

    public async Task<RequestForQuotationAwardResult?> GetByRequestForQuotationIdAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsRead, cancellationToken);
        if (requestForQuotationId == Guid.Empty)
        {
            throw new ArgumentException("RFQ identifier cannot be empty.", nameof(requestForQuotationId));
        }

        var award = await _repository.GetByRequestForQuotationIdAsync(requestForQuotationId, cancellationToken);
        return award is null ? null : RequestForQuotationAwardResult.FromDomain(award);
    }

    public async Task<RequestForQuotationAwardResult> UpdateDraftAsync(
        Guid awardId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsAward, cancellationToken);
        var award = await GetRequiredAwardAsync(awardId, cancellationToken);
        award.UpdateNotes(notes);
        await _repository.UpdateAsync(award, cancellationToken);
        return RequestForQuotationAwardResult.FromDomain(award);
    }

    public async Task<RequestForQuotationAwardResult> SelectLineAsync(
        Guid awardId,
        Guid requestForQuotationLineId,
        Guid supplierQuotationLineId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsAward, cancellationToken);
        var award = await GetRequiredAwardAsync(awardId, cancellationToken);
        var requestForQuotation = await GetRequiredRequestForQuotationAsync(
            award.RequestForQuotationId,
            cancellationToken);
        EnsureClosed(requestForQuotation);
        ValidateSelection(requestForQuotation, requestForQuotationLineId, supplierQuotationLineId);

        award.SelectLine(requestForQuotationLineId, supplierQuotationLineId);
        await _repository.UpdateAsync(award, cancellationToken);
        return RequestForQuotationAwardResult.FromDomain(award);
    }

    public async Task<RequestForQuotationAwardResult> RemoveLineSelectionAsync(
        Guid awardId,
        Guid requestForQuotationLineId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsAward, cancellationToken);
        var award = await GetRequiredAwardAsync(awardId, cancellationToken);
        award.RemoveLineSelection(requestForQuotationLineId);
        await _repository.UpdateAsync(award, cancellationToken);
        return RequestForQuotationAwardResult.FromDomain(award);
    }

    public async Task<RequestForQuotationAwardResult> FinalizeAsync(
        Guid awardId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsAward, cancellationToken);
        var award = await GetRequiredAwardAsync(awardId, cancellationToken);
        var requestForQuotation = await GetRequiredRequestForQuotationAsync(
            award.RequestForQuotationId,
            cancellationToken);
        EnsureClosed(requestForQuotation);

        award.Finalize(GetCurrentUserId(), _clock.UtcNow);
        await _repository.UpdateAsync(award, cancellationToken);
        return RequestForQuotationAwardResult.FromDomain(award);
    }

    private async Task<RequestForQuotationAward> GetRequiredAwardAsync(
        Guid awardId,
        CancellationToken cancellationToken)
    {
        if (awardId == Guid.Empty)
        {
            throw new ArgumentException("RFQ award identifier cannot be empty.", nameof(awardId));
        }

        return await _repository.GetByIdAsync(awardId, cancellationToken)
            ?? throw new KeyNotFoundException($"RFQ award decision '{awardId}' was not found.");
    }

    private async Task<RequestForQuotation> GetRequiredRequestForQuotationAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken)
    {
        if (requestForQuotationId == Guid.Empty)
        {
            throw new ArgumentException("RFQ identifier cannot be empty.", nameof(requestForQuotationId));
        }

        return await _requestForQuotationRepository.GetByIdAsync(requestForQuotationId, cancellationToken)
            ?? throw new KeyNotFoundException($"RFQ '{requestForQuotationId}' was not found.");
    }

    private static void EnsureClosed(RequestForQuotation requestForQuotation)
    {
        if (requestForQuotation.Status != RequestForQuotationStatus.Closed)
        {
            throw new InvalidOperationException("RFQ award decisions require a closed RFQ.");
        }
    }

    private static void ValidateSelection(
        RequestForQuotation requestForQuotation,
        Guid requestForQuotationLineId,
        Guid supplierQuotationLineId)
    {
        if (requestForQuotationLineId == Guid.Empty)
        {
            throw new ArgumentException("RFQ line identifier cannot be empty.", nameof(requestForQuotationLineId));
        }

        if (supplierQuotationLineId == Guid.Empty)
        {
            throw new ArgumentException(
                "Supplier quotation line identifier cannot be empty.",
                nameof(supplierQuotationLineId));
        }

        var requestForQuotationLine = requestForQuotation.Lines
            .SingleOrDefault(line => line.Id == requestForQuotationLineId)
            ?? throw new KeyNotFoundException(
                $"RFQ line '{requestForQuotationLineId}' was not found in RFQ '{requestForQuotation.Id}'.");

        SupplierQuotation? selectedQuotation = null;
        SupplierQuotationLine? selectedQuotationLine = null;
        foreach (var quotation in requestForQuotation.SupplierQuotations)
        {
            var quotationLine = quotation.Lines
                .SingleOrDefault(line => line.Id == supplierQuotationLineId);
            if (quotationLine is null)
            {
                continue;
            }

            selectedQuotation = quotation;
            selectedQuotationLine = quotationLine;
            break;
        }

        if (selectedQuotation is null || selectedQuotationLine is null)
        {
            throw new KeyNotFoundException(
                $"Supplier quotation line '{supplierQuotationLineId}' was not found in RFQ '{requestForQuotation.Id}'.");
        }

        if (selectedQuotation.Status != SupplierQuotationStatus.Finalized)
        {
            throw new InvalidOperationException("Only finalized supplier quotation lines can be awarded.");
        }

        if (selectedQuotationLine.RequestForQuotationLineId != requestForQuotationLine.Id)
        {
            throw new InvalidOperationException(
                "The supplier quotation line does not belong to the RFQ line being awarded.");
        }

        if (!requestForQuotation.Suppliers.Any(supplier => supplier.SupplierId == selectedQuotation.SupplierId))
        {
            throw new InvalidOperationException(
                "The selected supplier quotation does not belong to an invited RFQ supplier.");
        }
    }

    private Guid GetCurrentUserId()
    {
        var currentUser = _session.CurrentUser
            ?? throw new InvalidOperationException("RFQ award mutations require an authenticated session.");
        if (!currentUser.IsActive)
        {
            throw new InvalidOperationException("The authenticated user is not active.");
        }

        return currentUser.Id;
    }
}
