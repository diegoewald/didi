using LocalErp.Domain.Companies;
using LocalErp.Domain.Users;
using LocalErp.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermissionGrant> RolePermissions => Set<RolePermissionGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
    }
}
