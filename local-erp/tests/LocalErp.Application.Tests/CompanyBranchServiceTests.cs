using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Companies;
using LocalErp.Application.Security;
using LocalErp.Domain.Companies;
using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class CompanyBranchServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedBranchForExistingCompany()
    {
        var companies = new InMemoryCompanyRepository();
        var company = new Company("Empresa Exemplo");
        await companies.AddAsync(company);
        var branches = new InMemoryCompanyBranchRepository();
        var service = new CompanyBranchService(branches, companies, new AllowAllAuthorizationService());

        var result = await service.CreateAsync(company.Id, "  Filial Norte  ");

        Assert.AreEqual(company.Id, result.CompanyId);
        Assert.AreEqual("Filial Norte", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, branches.Branches.Count);
    }

    [TestMethod]
    public async Task ListAsyncDemandsCompaniesReadAndIncludesOnlyRequestedCompany()
    {
        var companies = new InMemoryCompanyRepository();
        var company = new Company("Empresa A");
        var anotherCompany = new Company("Empresa B");
        await companies.AddAsync(company);
        await companies.AddAsync(anotherCompany);
        var branches = new InMemoryCompanyBranchRepository();
        await branches.AddAsync(new CompanyBranch(company.Id, "Filial A"));
        await branches.AddAsync(new CompanyBranch(anotherCompany.Id, "Filial B"));
        var authorization = new RecordingAuthorizationService();
        var service = new CompanyBranchService(branches, companies, authorization);

        var result = await service.ListAsync(company.Id, includeInactive: true);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(company.Id, result[0].CompanyId);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.CompaniesRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task MutationsDemandCompaniesManage()
    {
        var companies = new InMemoryCompanyRepository();
        var company = new Company("Empresa");
        await companies.AddAsync(company);
        var branches = new InMemoryCompanyBranchRepository();
        var existing = new CompanyBranch(company.Id, "Filial Existente");
        await branches.AddAsync(existing);
        var authorization = new RecordingAuthorizationService();
        var service = new CompanyBranchService(branches, companies, authorization);

        await service.CreateAsync(company.Id, "Nova Filial");
        await service.RenameAsync(existing.Id, "Filial Renomeada");
        await service.SetActiveAsync(existing.Id, isActive: false);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.CompaniesManage,
                PermissionCatalog.CompaniesManage,
                PermissionCatalog.CompaniesManage,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task CreateAsyncRejectsUnknownCompanyBeforePersistingBranch()
    {
        var branches = new InMemoryCompanyBranchRepository();
        var service = new CompanyBranchService(
            branches,
            new InMemoryCompanyRepository(),
            new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => service.CreateAsync(Guid.NewGuid(), "Filial"));

        Assert.AreEqual(0, branches.Branches.Count);
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

    private sealed class InMemoryCompanyBranchRepository : ICompanyBranchRepository
    {
        public List<CompanyBranch> Branches { get; } = [];

        public Task AddAsync(CompanyBranch branch, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Branches.Add(branch);
            return Task.CompletedTask;
        }

        public Task<CompanyBranch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Branches.SingleOrDefault(branch => branch.Id == id));
        }

        public Task<IReadOnlyList<CompanyBranch>> ListByCompanyAsync(
            Guid companyId,
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<CompanyBranch> result = Branches
                .Where(branch => branch.CompanyId == companyId)
                .Where(branch => includeInactive || branch.IsActive)
                .OrderBy(branch => branch.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(CompanyBranch branch, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
