using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Companies;
using LocalErp.Domain.Companies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class CompanyServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedCompany()
    {
        var repository = new InMemoryCompanyRepository();
        var service = new CompanyService(repository, new AllowAllAuthorizationService());

        var result = await service.CreateAsync("  Empresa Exemplo  ");

        Assert.AreEqual("Empresa Exemplo", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, repository.Companies.Count);
    }

    [TestMethod]
    public async Task RenameAsyncPersistsDomainChange()
    {
        var repository = new InMemoryCompanyRepository();
        var company = new Company("Nome Antigo");
        await repository.AddAsync(company);
        var service = new CompanyService(repository, new AllowAllAuthorizationService());

        var result = await service.RenameAsync(company.Id, "  Nome Novo  ");

        Assert.AreEqual("Nome Novo", result.Name);
        Assert.AreEqual("Nome Novo", repository.Companies.Single().Name);
    }

    [TestMethod]
    public async Task SetActiveAsyncCanDeactivateCompany()
    {
        var repository = new InMemoryCompanyRepository();
        var company = new Company("Empresa Exemplo");
        await repository.AddAsync(company);
        var service = new CompanyService(repository, new AllowAllAuthorizationService());

        var result = await service.SetActiveAsync(company.Id, isActive: false);

        Assert.IsFalse(result.IsActive);
    }

    [TestMethod]
    public async Task RenameAsyncRejectsUnknownCompany()
    {
        var service = new CompanyService(new InMemoryCompanyRepository(), new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => service.RenameAsync(Guid.NewGuid(), "Novo Nome"));
    }

    private sealed class InMemoryCompanyRepository : ICompanyRepository
    {
        public List<Company> Companies { get; } = [];

        public Task AddAsync(Company company, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Companies.Add(company);
            return Task.CompletedTask;
        }

        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Companies.SingleOrDefault(company => company.Id == id));
        }

        public Task<IReadOnlyList<Company>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Company> result = Companies
                .Where(company => includeInactive || company.IsActive)
                .OrderBy(company => company.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
