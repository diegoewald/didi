using LocalErp.Domain.Companies;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfCompanyBranchRepositoryTests
{
    [TestMethod]
    public async Task RepositoryPersistsFiltersByCompanyAndUpdatesBranch()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var companyRepository = new EfCompanyRepository(testDatabase);
        var branchRepository = new EfCompanyBranchRepository(testDatabase);

        var company = new Company("Empresa A");
        var anotherCompany = new Company("Empresa B");
        await companyRepository.AddAsync(company);
        await companyRepository.AddAsync(anotherCompany);

        var active = new CompanyBranch(company.Id, "Filial Ativa");
        var inactive = new CompanyBranch(company.Id, "Filial Inativa");
        inactive.Deactivate();
        var other = new CompanyBranch(anotherCompany.Id, "Outra Filial");
        await branchRepository.AddAsync(active);
        await branchRepository.AddAsync(inactive);
        await branchRepository.AddAsync(other);

        var activeOnly = await branchRepository.ListByCompanyAsync(company.Id, includeInactive: false);
        Assert.AreEqual(1, activeOnly.Count);
        Assert.AreEqual(active.Id, activeOnly[0].Id);

        var all = await branchRepository.ListByCompanyAsync(company.Id, includeInactive: true);
        Assert.AreEqual(2, all.Count);
        Assert.IsTrue(all.All(branch => branch.CompanyId == company.Id));

        var loaded = await branchRepository.GetByIdAsync(active.Id)
            ?? throw new AssertFailedException("Persisted branch was not found.");
        loaded.Rename("Filial Renomeada");
        await branchRepository.UpdateAsync(loaded);

        var updated = await branchRepository.GetByIdAsync(active.Id)
            ?? throw new AssertFailedException("Updated branch was not found.");
        Assert.AreEqual("Filial Renomeada", updated.Name);
    }

    [TestMethod]
    public async Task RepositoryRejectsBranchForUnknownCompanyThroughForeignKey()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var branchRepository = new EfCompanyBranchRepository(testDatabase);
        var orphan = new CompanyBranch(Guid.NewGuid(), "Filial Órfã");

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => branchRepository.AddAsync(orphan));
    }

    private sealed class TestDatabase : IDbContextFactory<ErpDbContext>, IAsyncDisposable
    {
        private readonly string _directory;
        private readonly DbContextOptions<ErpDbContext> _options;

        private TestDatabase(string directory, DbContextOptions<ErpDbContext> options)
        {
            _directory = directory;
            _options = options;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var directory = Path.Combine(Path.GetTempPath(), "LocalErp", "Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var databasePath = Path.Combine(directory, "branches.db");
            var connectionString = $"{SqliteConnectionStringFactory.Create(databasePath)};Pooling=False";
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite(connectionString)
                .Options;

            var database = new TestDatabase(directory, options);
            await using var context = database.CreateDbContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public ErpDbContext CreateDbContext()
        {
            return new ErpDbContext(_options);
        }

        public Task<ErpDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
