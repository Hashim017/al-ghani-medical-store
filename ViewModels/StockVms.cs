using AlGhaniMedicalStore.Models;

namespace AlGhaniMedicalStore.ViewModels;

public record BatchOpt(int Id, string Text, int Qty);

public class AdjustVm
{
    public int BatchId { get; set; }
    public AdjustmentReason Reason { get; set; } = AdjustmentReason.Damaged;
    public int Change { get; set; }
    public string? Note { get; set; }
}

public class SaleReturnLine
{
    public int SaleItemId { get; set; }
    public string Medicine { get; set; } = "";
    public string Batch { get; set; } = "";
    public SellUnit Unit { get; set; }
    public int SoldQty { get; set; }
    public int ReturnedQty { get; set; }
    public decimal UnitRefund { get; set; }
    public int ReturnQty { get; set; }
}

public class SaleReturnVm
{
    public int SaleId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Total { get; set; }
    public string? Reason { get; set; }
    public List<SaleReturnLine> Lines { get; set; } = new();
}

public class PurchaseReturnVm
{
    public int SupplierId { get; set; }
    public int BatchId { get; set; }
    public int Tablets { get; set; }
    public decimal Credit { get; set; }
    public string? Reason { get; set; }
}