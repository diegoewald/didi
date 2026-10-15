using LocalErp.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Development;

public sealed class DevelopmentDataSeeder(IDbContextFactory<ErpDbContext> contextFactory)
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var pendingMigrations = await context.Database
            .GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            throw new InvalidOperationException(
                "Development seed requires an up-to-date database schema. Apply migrations first.");
        }

        if (await context.Companies.AnyAsync(cancellationToken))
        {
            return 0;
        }

        var inactiveCompany = new Company("Empresa Inativa de Exemplo");
        inactiveCompany.Deactivate();

        context.Companies.AddRange(
            new Company("Empresa Demonstração"),
            inactiveCompany);

        return await context.SaveChangesAsync(cancellationToken);
    }
}
