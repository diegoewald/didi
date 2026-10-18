using LocalErp.Domain.Companies;
using LocalErp.Domain.Materials;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<CompanyBranch> CompanyBranches => Set<CompanyBranch>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermissionGrant> RolePermissions => Set<RolePermissionGrant>();

    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
    }
}
