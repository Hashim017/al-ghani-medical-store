using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class PurchasesController : Controller
{
    private readonly AppDbContext _db;
    public PurchasesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Purchases
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Id)
            .Take(100)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await _db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Medicine)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return View(p);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLists();
        return View(new PurchaseCreateVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseCreateVm vm)
    {
        if (vm.SupplierId > 0 && !await _db.Suppliers.AnyAsync(s => s.Id == vm.SupplierId))
            ModelState.AddModelError(nameof(vm.SupplierId), "Supplier not found.");

        if (vm.Lines.Count == 0)
            ModelState.AddModelError("", "Add at least one medicine.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var medIds = vm.Lines.Select(l => l.MedicineId).Distinct().ToList();
        var meds = await _db.Medicines.Where(m => medIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);
        var existing = await _db.Batches.Where(b => medIds.Contains(b.MedicineId)).ToListAsync();

        var pending = new Dictionary<(int, string), Batch>();
        var purchase = new Purchase
        {
            SupplierId = vm.SupplierId,
            PurchaseDate = vm.PurchaseDate,
            BillNumber = string.IsNullOrWhiteSpace(vm.BillNumber) ? null : vm.BillNumber.Trim(),
            PaymentType = vm.PaymentType
        };
        decimal total = 0;

        for (int i = 0; i < vm.Lines.Count; i++)
        {
            var line = vm.Lines[i];
            var row = i + 1;

            if (!meds.TryGetValue(line.MedicineId, out var med))
            {
                ModelState.AddModelError("", $"Row {row}: medicine not found.");
                continue;
            }

            var batchNo = (line.BatchNumber ?? "").Trim();
            if (batchNo == "")
            {
                ModelState.AddModelError("", $"Row {row}: batch number is required.");
                continue;
            }
            if (line.Quantity < 1)
            {
                ModelState.AddModelError("", $"Row {row}: quantity must be at least 1.");
                continue;
            }
            if (line.UnitCost < 0)
            {
                ModelState.AddModelError("", $"Row {row}: cost cannot be negative.");
                continue;
            }
            if (line.ExpiryDate <= today)
            {
                ModelState.AddModelError("", $"Row {row}: expiry date must be in the future.");
                continue;
            }

            int perUnit = line.Unit switch
            {
                SellUnit.Box => med.StripsPerBox * med.TabletsPerStrip,
                SellUnit.Strip => med.TabletsPerStrip,
                _ => 1
            };
            int tablets = line.Quantity * perUnit;
            decimal costPerTablet = Math.Round(line.UnitCost / perUnit, 4);

            var key = (med.Id, batchNo.ToUpperInvariant());
            if (!pending.TryGetValue(key, out var batch))
            {
                batch = existing.FirstOrDefault(b => b.MedicineId == med.Id &&
                    string.Equals(b.BatchNumber, batchNo, StringComparison.OrdinalIgnoreCase));

                if (batch == null)
                {
                    batch = new Batch
                    {
                        MedicineId = med.Id,
                        BatchNumber = batchNo,
                        ExpiryDate = line.ExpiryDate,
                        CostPricePerTablet = costPerTablet,
                        QuantityLeft = 0
                    };
                    _db.Batches.Add(batch);
                }
                else if (batch.ExpiryDate != line.ExpiryDate)
                {
                    ModelState.AddModelError("",
                        $"Row {row}: batch {batchNo} already exists with expiry {batch.ExpiryDate:dd MMM yyyy}.");
                    continue;
                }
                else
                {
                    batch.CostPricePerTablet = costPerTablet;
                }
                pending[key] = batch;
            }

            batch.QuantityLeft += tablets;

            var lineTotal = Math.Round(line.Quantity * line.UnitCost, 2);
            total += lineTotal;

            purchase.Items.Add(new PurchaseItem
            {
                Batch = batch,
                UnitBought = line.Unit,
                Quantity = line.Quantity,
                TabletsAdded = tablets,
                UnitCost = line.UnitCost,
                LineTotal = lineTotal
            });
        }

        if (!ModelState.IsValid)
        {
            // Throw away the changes that were not saved
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            await LoadLists();
            return View(vm);
        }

        purchase.Total = total;
        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync();   // one save, so all or nothing

        return RedirectToAction(nameof(Details), new { id = purchase.Id });
    }

    private async Task LoadLists()
    {
        ViewBag.Suppliers = new SelectList(
            await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(),
            "Id", "Name");

        ViewBag.Meds = await _db.Medicines
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new MedOption(m.Id, m.Name, m.UsesStrips))
            .ToListAsync();
    }
}