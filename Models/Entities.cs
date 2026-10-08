using Microsoft.AspNetCore.Identity;

namespace AlGhaniMedicalStore.Models;

public enum PaymentType { Cash = 1, Online = 2 }
public enum SellUnit { Box = 1, Strip = 2, Tablet = 3 }
public enum AdjustmentReason { Damaged = 1, Expired = 2, CountCorrection = 3, Other = 4 }

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Generic
{
    public int Id { get; set; }
    public string Name { get; set; } = "";   // e.g. Paracetamol
}

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Medicine
{
    public int Id { get; set; }
    public string Name { get; set; } = "";      // e.g. Panadol 500mg
    public string? Form { get; set; }           // Tablet, Syrup, Injection
    public string? RackLocation { get; set; }
    public int ReorderLevel { get; set; }       // in tablets
    public bool IsActive { get; set; } = true;
    public bool UsesStrips { get; set; } = true;

    // Unit setup
    public int StripsPerBox { get; set; } = 1;
    public int TabletsPerStrip { get; set; } = 1;
    public decimal BoxPrice { get; set; }
    public decimal StripPrice { get; set; }
    public decimal TabletPrice { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? GenericId { get; set; }
    public Generic? Generic { get; set; }

    public List<Batch> Batches { get; set; } = new();
}

public class Batch
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public string BatchNumber { get; set; } = "";
    public DateOnly ExpiryDate { get; set; }
    public decimal CostPricePerTablet { get; set; }
    public int QuantityLeft { get; set; }       // in tablets
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class StockAdjustment
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public Batch? Batch { get; set; }
    public int QuantityChange { get; set; }     // negative removes stock, in tablets
    public AdjustmentReason Reason { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UserId { get; set; }
}

public class Purchase
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    public string? BillNumber { get; set; }
    public decimal Total { get; set; }
    public PaymentType PaymentType { get; set; }
    public string? UserId { get; set; }
    public List<PurchaseItem> Items { get; set; } = new();
}

public class PurchaseItem
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public int BatchId { get; set; }
    public Batch? Batch { get; set; }
    public SellUnit UnitBought { get; set; }
    public int Quantity { get; set; }           // in the unit bought
    public int TabletsAdded { get; set; }       // converted to tablets
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class Sale
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public PaymentType PaymentType { get; set; }
    public string? UserId { get; set; }
    public List<SaleItem> Items { get; set; } = new();
}

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public int BatchId { get; set; }
    public Batch? Batch { get; set; }
    public SellUnit UnitSold { get; set; }
    public int Quantity { get; set; }           // in the unit sold
    public int TabletsDeducted { get; set; }    // converted to tablets
    public decimal UnitPrice { get; set; }
    public decimal CostPerTablet { get; set; }  // copied from batch, for profit reports
    public decimal LineTotal { get; set; }
}

public class SaleReturn
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.Now;
    public decimal RefundTotal { get; set; }
    public string? Reason { get; set; }
    public string? UserId { get; set; }
    public List<SaleReturnItem> Items { get; set; } = new();
}

public class SaleReturnItem
{
    public int Id { get; set; }
    public int SaleReturnId { get; set; }
    public SaleReturn? SaleReturn { get; set; }
    public int SaleItemId { get; set; }
    public SaleItem? SaleItem { get; set; }
    public int TabletsReturned { get; set; }
    public decimal RefundAmount { get; set; }
}

public class PurchaseReturn
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime ReturnDate { get; set; } = DateTime.Now;
    public decimal CreditTotal { get; set; }
    public string? Reason { get; set; }
    public string? UserId { get; set; }
    public List<PurchaseReturnItem> Items { get; set; } = new();
}

public class PurchaseReturnItem
{
    public int Id { get; set; }
    public int PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }
    public int BatchId { get; set; }
    public Batch? Batch { get; set; }
    public int TabletsReturned { get; set; }
    public decimal CreditAmount { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = "";    // Created, Updated, Deleted
    public string EntityName { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Setting
{
    public int Id { get; set; }
    public string ShopName { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? BillFooter { get; set; }
    public int ExpiryAlertDays { get; set; } = 60;
}

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
}