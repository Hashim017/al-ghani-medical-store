using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AlGhaniMedicalStore.Models;

namespace AlGhaniMedicalStore.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Generic> Generics => Set<Generic>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnItem> SaleReturnItems => Set<SaleReturnItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Money and price columns
        foreach (var prop in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal)))
        {
            prop.SetPrecision(18);
            prop.SetScale(2);
        }

        // Cost per tablet needs more decimals
        b.Entity<Batch>().Property(x => x.CostPricePerTablet).HasPrecision(18, 4);
        b.Entity<SaleItem>().Property(x => x.CostPerTablet).HasPrecision(18, 4);

        // Never delete a parent that has children. Keeps history safe.
        foreach (var fk in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Fast search as you type
        b.Entity<Medicine>().HasIndex(m => m.Name);

        // Same batch number cannot repeat for one medicine
        b.Entity<Batch>()
            .HasIndex(x => new { x.MedicineId, x.BatchNumber })
            .IsUnique();

        // Fast expiry alerts and FEFO
        b.Entity<Batch>().HasIndex(x => x.ExpiryDate);

        // Reports by date
        b.Entity<Sale>().HasIndex(s => s.SaleDate);
        b.Entity<Purchase>().HasIndex(p => p.PurchaseDate);
    }
}