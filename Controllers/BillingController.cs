using System.Security.Claims;
using System.Text.Json;
using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class BillingController : Controller
{
    private readonly AppDbContext _db;
    public BillingController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        await LoadSettings();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? q)
    {
        q = (q ?? "").Trim();
        if (q.Length < 2) return Json(new List<object>());

        var today = DateOnly.FromDateTime(DateTime.Today);

        var list = await _db.Medicines
            .Where(m => m.IsActive &&
                (m.Name.Contains(q) || (m.Generic != null && m.Generic.Name.Contains(q))))
            .OrderByDescending(m => m.Name.StartsWith(q))
            .ThenBy(m => m.Name)
            .Take(15)
            .Select(m => new
            {
                id = m.Id,
                name = m.Name,
                generic = m.Generic != null ? m.Generic.Name : "",
                form = m.Form,
                rack = m.RackLocation,
                stripsPerBox = m.StripsPerBox,
                tabletsPerStrip = m.TabletsPerStrip,
                boxPrice = m.BoxPrice,
                stripPrice = m.StripPrice,
                tabletPrice = m.TabletPrice,
                stock = m.Batches
                    .Where(b => b.ExpiryDate > today && b.QuantityLeft > 0)
                    .Sum(b => b.QuantityLeft),
                expiry = m.Batches
                    .Where(b => b.ExpiryDate > today && b.QuantityLeft > 0)
                    .Min(b => (DateOnly?)b.ExpiryDate)
            })
            .ToListAsync();

        return Json(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string? cartJson, decimal discount, PaymentType paymentType)
    {
        List<CartLine> cart;
        try
        {
            cart = JsonSerializer.Deserialize<List<CartLine>>(
                cartJson ?? "[]",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            cart = new();
        }

        if (cart.Count == 0)
            return await Fail("The cart is empty.", cartJson, discount, paymentType);
        if (discount < 0)
            return await Fail("Discount cannot be negative.", cartJson, discount, paymentType);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var medIds = cart.Select(c => c.MedicineId).Distinct().ToList();

        var meds = await _db.Medicines
            .Where(m => medIds.Contains(m.Id) && m.IsActive)
            .ToDictionaryAsync(m => m.Id);

        // Only good batches, earliest expiry first
        var batches = await _db.Batches
            .Where(b => medIds.Contains(b.MedicineId) && b.QuantityLeft > 0 && b.ExpiryDate > today)
            .OrderBy(b => b.ExpiryDate).ThenBy(b => b.Id)
            .ToListAsync();

        var sale = new Sale
        {
            SaleDate = DateTime.Now,
            PaymentType = paymentType,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        foreach (var line in cart)
        {
            if (!meds.TryGetValue(line.MedicineId, out var med))
                return await Fail("A medicine was not found or is turned off.", cartJson, discount, paymentType);

            if (line.Quantity < 1)
                return await Fail($"{med.Name}: quantity must be at least 1.", cartJson, discount, paymentType);

            if (!Enum.IsDefined(typeof(SellUnit), line.Unit))
                return await Fail($"{med.Name}: unit is not valid.", cartJson, discount, paymentType);

            int perUnit = line.Unit switch
            {
                SellUnit.Box => med.StripsPerBox * med.TabletsPerStrip,
                SellUnit.Strip => med.TabletsPerStrip,
                _ => 1
            };

            decimal price = line.Unit switch
            {
                SellUnit.Box => med.BoxPrice,
                SellUnit.Strip => med.StripPrice,
                _ => med.TabletPrice
            };

            if (price <= 0)
                return await Fail($"{med.Name}: the {line.Unit} price is not set.", cartJson, discount, paymentType);

            int need = line.Quantity;

            foreach (var b in batches.Where(x => x.MedicineId == med.Id))
            {
                if (need == 0) break;

                int unitsInBatch = b.QuantityLeft / perUnit;
                if (unitsInBatch < 1) continue;

                int take = Math.Min(unitsInBatch, need);
                b.QuantityLeft -= take * perUnit;
                need -= take;

                sale.Items.Add(new SaleItem
                {
                    Batch = b,
                    UnitSold = line.Unit,
                    Quantity = take,
                    TabletsDeducted = take * perUnit,
                    UnitPrice = price,
                    CostPerTablet = b.CostPricePerTablet,
                    LineTotal = Math.Round(take * price, 2)
                });
            }

            if (need > 0)
                return await Fail(
                    $"{med.Name}: not enough stock in {line.Unit} units. Try a smaller unit.",
                    cartJson, discount, paymentType);
        }

        decimal subTotal = sale.Items.Sum(i => i.LineTotal);
        discount = Math.Round(discount, 2);

        if (discount > subTotal)
            return await Fail("Discount is more than the bill total.", cartJson, discount, paymentType);

        sale.SubTotal = subTotal;
        sale.Discount = discount;
        sale.Total = subTotal - discount;

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();   // one save, so all or nothing

        return RedirectToAction(nameof(Receipt), new { id = sale.Id });
    }

    public async Task<IActionResult> Receipt(int id)
    {
        var sale = await _db.Sales
            .Include(s => s.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Medicine)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null) return NotFound();

        ViewBag.Shop = await _db.Settings.FirstOrDefaultAsync();
        return View(sale);
    }

    public async Task<IActionResult> History()
    {
        var list = await _db.Sales
            .Include(s => s.Items)
            .OrderByDescending(s => s.SaleDate).ThenByDescending(s => s.Id)
            .Take(100)
            .ToListAsync();
        return View(list);
    }

    private async Task<IActionResult> Fail(string message, string? cartJson, decimal discount, PaymentType pay)
    {
        // Throw away stock changes that were not saved
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;

        await LoadSettings();
        ViewBag.Error = message;
        ViewBag.CartJson = cartJson;
        ViewBag.Discount = discount;
        ViewBag.Payment = (int)pay;
        return View("Index");
    }

    private async Task LoadSettings()
    {
        var days = await _db.Settings.Select(s => s.ExpiryAlertDays).FirstOrDefaultAsync();
        ViewBag.AlertDays = days > 0 ? days : 60;
    }
}