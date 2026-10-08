using System.Security.Claims;
using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class StockAdjustmentsController : Controller
{
    private readonly AppDbContext _db;
    public StockAdjustmentsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.StockAdjustments
            .Include(a => a.Batch).ThenInclude(b => b!.Medicine)
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Take(100)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create(int? batchId)
    {
        var vm = new AdjustVm();
        if (batchId.HasValue)
        {
            var b = await _db.Batches.FindAsync(batchId.Value);
            if (b != null)
            {
                vm.BatchId = b.Id;
                if (b.ExpiryDate <= DateOnly.FromDateTime(DateTime.Today))
                {
                    vm.Reason = AdjustmentReason.Expired;
                    vm.Change = -b.QuantityLeft;
                }
            }
        }
        await LoadBatches();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdjustVm vm)
    {
        ModelState.Remove(nameof(AdjustVm.Change));
        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == vm.BatchId);

        if (batch == null)
            ModelState.AddModelError("", "Select a batch.");
        else if (vm.Change == 0)
            ModelState.AddModelError("", "Change cannot be zero.");
        else if (batch.QuantityLeft + vm.Change < 0)
            ModelState.AddModelError("", $"Only {batch.QuantityLeft} tablets are left in this batch.");

        if (!ModelState.IsValid)
        {
            await LoadBatches();
            return View(vm);
        }

        batch!.QuantityLeft += vm.Change;
        _db.StockAdjustments.Add(new StockAdjustment
        {
            BatchId = batch.Id,
            QuantityChange = vm.Change,
            Reason = vm.Reason,
            Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim(),
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        });
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Stock adjusted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadBatches()
    {
        var rows = await _db.Batches
            .Where(b => b.QuantityLeft > 0)
            .OrderBy(b => b.Medicine!.Name).ThenBy(b => b.ExpiryDate)
            .Select(b => new { b.Id, Name = b.Medicine!.Name, b.BatchNumber, b.ExpiryDate, b.QuantityLeft })
            .ToListAsync();

        ViewBag.Batches = rows
            .Select(r => new BatchOpt(r.Id,
                $"{r.Name} | batch {r.BatchNumber} | exp {r.ExpiryDate:dd MMM yyyy} | {r.QuantityLeft} left",
                r.QuantityLeft))
            .ToList();
    }
}