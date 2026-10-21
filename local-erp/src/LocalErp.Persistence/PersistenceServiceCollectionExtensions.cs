using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Persistence.Development;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalErp.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddLocalErpPersistence(
        this IServiceCollection services,
        string databasePath)
    {
        ArgumentNullException.ThrowIfNull(services);

        var connectionString = SqliteConnectionStringFactory.Create(databasePath);

        services.AddSingleton(new SqliteDatabaseRuntimeConfigurator(connectionString));

        services.AddDbContextFactory<ErpDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddTransient<ICategoryRepository, EfCategoryRepository>();
        services.AddTransient<ICompanyRepository, EfCompanyRepository>();
        services.AddTransient<ICompanyBranchRepository, EfCompanyBranchRepository>();
        services.AddTransient<ICostCenterRepository, EfCostCenterRepository>();
        services.AddTransient<IMaterialRepository, EfMaterialRepository>();
        services.AddTransient<IProductRepository, EfProductRepository>();
        services.AddTransient<ISupplierRepository, EfSupplierRepository>();
        services.AddTransient<IMeasurementUnitRepository, EfMeasurementUnitRepository>();
        services.AddTransient<IUserRepository, EfUserRepository>();
        services.AddTransient<IRoleRepository, EfRoleRepository>();
        services.AddTransient<IAuthorizationRepository, EfAuthorizationRepository>();
        services.AddTransient<IAdministratorBootstrapStore, SqliteAdministratorBootstrapStore>();
        services.AddTransient<IAuthenticationAttemptStore, SqliteAuthenticationAttemptStore>();
        services.AddTransient<IAdministrativeAuditStore, SqliteAdministrativeAuditStore>();
        services.AddTransient<IUserCredentialStore, SqliteUserCredentialStore>();
        services.AddTransient<ISecurityAuditStore, EfSecurityAuditStore>();
        services.AddTransient<DevelopmentDataSeeder>();

        return services;
    }
}
