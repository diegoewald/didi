using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Application.Units;
using LocalErp.Domain.Security;
using LocalErp.Domain.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class MeasurementUnitServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedMeasurementUnit()
    {
        var repository = new InMemoryMeasurementUnitRepository();
        var service = new MeasurementUnitService(repository, new AllowAllAuthorizationService());

        var result = await service.CreateAsync("  Quilograma  ");

        Assert.AreEqual("Quilograma", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, repository.Units.Count);
    }

    [TestMethod]
    public async Task ListAsyncDemandsUnitsRead()
    {
        var authorization = new RecordingAuthorizationService();
        var service = new MeasurementUnitService(new InMemoryMeasurementUnitRepository(), authorization);

        await service.ListAsync(includeInactive: true);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.UnitsRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task MutationsDemandUnitsManage()
    {
        var repository = new InMemoryMeasurementUnitRepository();
        var unit = new MeasurementUnit("Quilograma");
        await repository.AddAsync(unit);
        var authorization = new RecordingAuthorizationService();
        var service = new MeasurementUnitService(repository, authorization);

        await service.CreateAsync("Unidade");
        await service.RenameAsync(unit.Id, "Quilograma Renomeado");
        await service.SetActiveAsync(unit.Id, isActive: false);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.UnitsManage,
                PermissionCatalog.UnitsManage,
                PermissionCatalog.UnitsManage,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task RenameAndStatusChangesPersistDomainState()
    {
        var repository = new InMemoryMeasurementUnitRepository();
        var unit = new MeasurementUnit("Nome Antigo");
        await repository.AddAsync(unit);
        var service = new MeasurementUnitService(repository, new AllowAllAuthorizationService());

        var renamed = await service.RenameAsync(unit.Id, "  Nome Novo  ");
        var deactivated = await service.SetActiveAsync(unit.Id, isActive: false);

        Assert.AreEqual("Nome Novo", renamed.Name);
        Assert.AreEqual("Nome Novo", repository.Units.Single().Name);
        Assert.IsFalse(deactivated.IsActive);
    }

    [TestMethod]
    public async Task RenameAsyncRejectsUnknownMeasurementUnit()
    {
        var service = new MeasurementUnitService(
            new InMemoryMeasurementUnitRepository(),
            new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => service.RenameAsync(Guid.NewGuid(), "Novo Nome"));
    }

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        public List<string> Demands { get; } = [];

        public Task<bool> IsAllowedAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task DemandAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Demands.Add(permissionCode);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMeasurementUnitRepository : IMeasurementUnitRepository
    {
        public List<MeasurementUnit> Units { get; } = [];

        public Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Units.Add(unit);
            return Task.CompletedTask;
        }

        public Task<MeasurementUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Units.SingleOrDefault(unit => unit.Id == id));
        }

        public Task<IReadOnlyList<MeasurementUnit>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<MeasurementUnit> result = Units
                .Where(unit => includeInactive || unit.IsActive)
                .OrderBy(unit => unit.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
