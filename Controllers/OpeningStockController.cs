using System.Security.Claims;
using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.Services;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class OpeningStockController : Controller
{
    private readonly AppDbContext _db;
    public OpeningStockController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Create(int? medicineId)
    {
        await LoadMeds();
        ViewBag.StartId = medicineId ?? 0;
        return View(new OpeningVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpeningVm vm)
    {
        if (vm.Lines.Count == 0)
            ModelState.AddModelError("", "Add at least one medicine.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var medIds = vm.Lines.Select(l => l.MedicineId).Distinct().ToList();
        var meds = await _db.Medicines.Where(m => medIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);
        var existing = await _db.Batches.Where(b => medIds.Contains(b.MedicineId)).ToListAsync();
        var pending = new Dictionary<(int, string), Batch>();

        for (int i = 0; i < vm.Lines.Count; i++)
        {
            var line = vm.Lines[i];
            var row = i + 1;

            if (!meds.TryGetValue(line.MedicineId, out var med))
            {
                ModelState.AddModelError("", $"Row {row}: medicine not found.");
                continue;
            }
            if (line.Quantity < 1)
            {
                ModelState.AddModelError("", $"Row {row}: quantity must be at least 1.");
                continue;
            }
            if (line.UnitCost < 0 || line.SellPrice < 0)
            {
                ModelState.AddModelError("", $"Row {row}: prices cannot be negative.");
                continue;
            }
            if (line.ExpiryDate <= today)
            {
                ModelState.AddModelError("", $"Row {row}: expiry date must be in the future.");
                continue;
            }

            int perUnit = StockHelper.TabletsPer(med, line.Unit);
            int tablets = line.Quantity * perUnit;
            decimal costPerTablet = Math.Round(line.UnitCost / perUnit, 4);

            var batchNo = (line.BatchNumber ?? "").Trim();
            if (batchNo == "")
            {
                int n = 1;
                while (existing.Any(b => b.MedicineId == med.Id && b.BatchNumber == "OPEN-" + n) ||
                       pending.ContainsKey((med.Id, ("OPEN-" + n).ToUpperInvariant())))
                    n++;
                batchNo = "OPEN-" + n;
            }

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

            _db.StockAdjustments.Add(new StockAdjustment
            {
                Batch = batch,
                QuantityChange = tablets,
                Reason = AdjustmentReason.CountCorrection,
                Note = "Opening stock",
                UserId = userId
            });

            if (line.SellPrice > 0)
                StockHelper.ApplyPrice(med, line.Unit, line.SellPrice.Value);
        }

        if (!ModelState.IsValid)
        {
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            await LoadMeds();
            ViewBag.StartId = 0;
            return View(vm);
        }

        await _db.SaveChangesAsync();
        TempData["Ok"] = "Stock added.";
        return RedirectToAction("Index", "Medicines");
    }

    private async Task LoadMeds()
    {
        ViewBag.Opening = true;
        ViewBag.Meds = await _db.Medicines
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new MedOption(m.Id, m.Name, m.UsesStrips, m.BoxPrice, m.StripPrice, m.TabletPrice))
            .ToListAsync();
    }
}