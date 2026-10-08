using System.ComponentModel.DataAnnotations;
using AlGhaniMedicalStore.Models;

namespace AlGhaniMedicalStore.ViewModels;

public record MedOption(int Id, string Name, bool UsesStrips);

public class PurchaseCreateVm
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a supplier.")]
    public int SupplierId { get; set; }

    public string? BillNumber { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    public PaymentType PaymentType { get; set; } = PaymentType.Cash;

    public List<PurchaseLineVm> Lines { get; set; } = new();
}

public class PurchaseLineVm
{
    public int MedicineId { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public SellUnit Unit { get; set; } = SellUnit.Strip;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}