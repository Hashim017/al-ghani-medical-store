using AlGhaniMedicalStore.Models;

namespace AlGhaniMedicalStore.ViewModels;

public class CartLine
{
    public int MedicineId { get; set; }
    public SellUnit Unit { get; set; }
    public int Quantity { get; set; }
}