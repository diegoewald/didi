using LocalErp.Domain.Companies;
using LocalErp.Persistence.Development;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class DevelopmentDataSeederTests
{
    [TestMethod]
    public async Task SeedAsyncAddsDemonstrationCompaniesOnlyOnce()
    {
        await using var database = await TestDatabase.CreateAsync(applyMigrations: true);
        var seeder = new DevelopmentDataSeeder(database);

        var firstInsertCount = await seeder.SeedAsync();
        var secondInsertCount = await seeder.SeedAsync();

        await using var context = database.CreateDbContext();
        var companies = await context.Companies.AsNoTracking().OrderBy(company => company.Name).ToListAsync();

        Assert.AreEqual(2, firstInsertCount);
        Assert.AreEqual(0, secondInsertCount);
        Assert.AreEqual(2, companies.Count);
        Assert.IsTrue(companies.Any(company => company.Name == "Empresa Demonstração" && company.IsActive));
        Assert.IsTrue(companies.Any(company => company.Name == "Empresa Inativa de Exemplo" && !company.IsActive));
    }

    [TestMethod]
    public async Task SeedAsyncDoesNotModifyDatabaseWithExistingBusinessData()
    {
        await using var database = await TestDatabase.CreateAsync(applyMigrations: true);
        await using (var context = database.CreateDbContext())
        {
            context.Companies.Add(new Company("Empresa Existente"));
            await context.SaveChangesAsync();
        }

        var inserted = await new DevelopmentDataSeeder(database).SeedAsync();

        await using var verificationContext = database.CreateDbContext();
        var companies = await verificationContext.Companies.AsNoTracking().ToListAsync();
        Assert.AreEqual(0, inserted);
        Assert.AreEqual(1, companies.Count);
        Assert.AreEqual("Empresa Existente", companies.Single().Name);
    }

    [TestMethod]
    public async Task SeedAsyncRejectsDatabaseWithPendingMigrations()
    {
        await using var database = await TestDatabase.CreateAsync(applyMigrations: false);
        var seeder = new DevelopmentDataSeeder(database);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => seeder.SeedAsync());

        Assert.IsTrue(exception.Message.Contains("Apply migrations first", StringComparison.Ordinal));
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

        public static async Task<TestDatabase> CreateAsync(bool applyMigrations)
        {
            var directory = Path.Combine(Path.GetTempPath(), "LocalErp", "Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var databasePath = Path.Combine(directory, "seed.db");
            var connectionString = $"{SqliteConnectionStringFactory.Create(databasePath)};Pooling=False";
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite(connectionString)
                .Options;

            var database = new TestDatabase(directory, options);
            if (applyMigrations)
            {
                await using var context = database.CreateDbContext();
                await context.Database.MigrateAsync();
            }

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
