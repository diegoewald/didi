using LocalErp.Domain.Categories;
using LocalErp.Domain.Companies;
using LocalErp.Domain.CostCenters;
using LocalErp.Domain.Materials;
using LocalErp.Domain.Products;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Units;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<CompanyBranch> CompanyBranches => Set<CompanyBranch>();

    public DbSet<CostCenter> CostCenters => Set<CostCenter>();

    public DbSet<Material> Materials => Set<Material>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();

    public DbSet<PurchaseRequestLine> PurchaseRequestLines => Set<PurchaseRequestLine>();

    public DbSet<RequestForQuotation> RequestForQuotations => Set<RequestForQuotation>();

    public DbSet<RequestForQuotationLine> RequestForQuotationLines => Set<RequestForQuotationLine>();

    public DbSet<RequestForQuotationSupplier> RequestForQuotationSuppliers => Set<RequestForQuotationSupplier>();

    public DbSet<RequestForQuotationAward> RequestForQuotationAwards => Set<RequestForQuotationAward>();

    public DbSet<RequestForQuotationAwardLine> RequestForQuotationAwardLines => Set<RequestForQuotationAwardLine>();

    public DbSet<SupplierQuotation> SupplierQuotations => Set<SupplierQuotation>();

    public DbSet<SupplierQuotationLine> SupplierQuotationLines => Set<SupplierQuotationLine>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();

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
