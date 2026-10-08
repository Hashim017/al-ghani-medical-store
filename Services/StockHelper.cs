using AlGhaniMedicalStore.Models;

namespace AlGhaniMedicalStore.Services;

public static class StockHelper
{
    public static int TabletsPer(Medicine m, SellUnit unit) => unit switch
    {
        SellUnit.Box => m.StripsPerBox * m.TabletsPerStrip,
        SellUnit.Strip => m.TabletsPerStrip,
        _ => 1
    };

    // The price typed for one unit sets that unit. The other units follow by division.
    public static void ApplyPrice(Medicine m, SellUnit unit, decimal price)
    {
        decimal perTablet = price / TabletsPer(m, unit);

        m.TabletPrice = unit == SellUnit.Tablet ? price : Math.Round(perTablet, 2);
        m.StripPrice = unit == SellUnit.Strip ? price : Math.Round(perTablet * m.TabletsPerStrip, 2);
        m.BoxPrice = unit == SellUnit.Box ? price : Math.Round(perTablet * m.StripsPerBox * m.TabletsPerStrip, 2);
    }
}