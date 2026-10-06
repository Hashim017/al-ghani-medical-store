namespace AlGhaniMedicalStore.ViewModels;

public class ExpiryRow
{
	public int BatchId { get; set; }
	public string MedicineName { get; set; } = "";
	public string BatchNumber { get; set; } = "";
	public DateOnly ExpiryDate { get; set; }
	public int QuantityLeft { get; set; }
	public decimal CostValue { get; set; }
	public int DaysLeft { get; set; }
}

public class LowStockRow
{
	public string Name { get; set; } = "";
	public string? Rack { get; set; }
	public int Stock { get; set; }
	public int ReorderLevel { get; set; }
}

public class TopItem
{
	public string Name { get; set; } = "";
	public int Tablets { get; set; }
	public decimal Amount { get; set; }
}

public class DailyReportVm
{
	public DateTime Date { get; set; }
	public int Bills { get; set; }
	public decimal SubTotal { get; set; }
	public decimal Discount { get; set; }
	public decimal SalesTotal { get; set; }
	public decimal Cash { get; set; }
	public decimal Online { get; set; }
	public decimal Profit { get; set; }
	public decimal PurchaseCash { get; set; }
	public decimal PurchaseOnline { get; set; }
	public decimal PurchaseTotal => PurchaseCash + PurchaseOnline;
    public decimal Refunds { get; set; }
    public decimal SupplierCredit { get; set; }
    public decimal NetSales => SalesTotal - Refunds;
    public decimal NetCash => Cash - Refunds - PurchaseCash + SupplierCredit;
    public List<TopItem> Top { get; set; } = new();
}

public class DashboardVm
{
	public int AlertDays { get; set; }
	public DailyReportVm Today { get; set; } = new();
	public int ExpiringCount { get; set; }
	public int ExpiredCount { get; set; }
	public int LowStockCount { get; set; }
	public List<ExpiryRow> Expiring { get; set; } = new();
	public List<LowStockRow> LowStock { get; set; } = new();
	public List<DayPoint> Week { get; set; } = new();
}

public class ExpiryPageVm
{
	public int Days { get; set; }
	public List<ExpiryRow> Expired { get; set; } = new();
	public List<ExpiryRow> Expiring { get; set; } = new();
}

public class DayPoint
{
	public string Label { get; set; } = "";
	public decimal Total { get; set; }
}