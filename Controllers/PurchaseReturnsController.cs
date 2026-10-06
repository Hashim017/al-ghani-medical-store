using System.Security.Claims;
using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class PurchaseReturnsController : Controller
{
    private readonly AppDbContext _db;
    public PurchaseReturnsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.PurchaseReturns
            .Include(r => r.Supplier)
            .Include(r => r.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Medicine)
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
            .Take(100)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create(int? batchId)
    {
        var vm = new PurchaseReturnVm();
        if (batchId.HasValue)
        {
            var b = await _db.Batches.FindAsync(batchId.Value);
            if (b != null)
            {
                vm.BatchId = b.Id;
                vm.Tablets = b.QuantityLeft;
                if (b.ExpiryDate <= DateOnly.FromDateTime(DateTime.Today))
                    vm.Reason = "Expired";
            }
        }
        await LoadLists();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseReturnVm vm)
    {
        var supplier = await _db.Suppliers.FindAsync(vm.SupplierId);
        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == vm.BatchId);

        if (supplier == null) ModelState.AddModelError("", "Select a supplier.");
        if (batch == null) ModelState.AddModelError("", "Select a batch.");
        else if (vm.Tablets < 1 || vm.Tablets > batch.QuantityLeft)
            ModelState.AddModelError("", $"Tablets must be between 1 and {batch.QuantityLeft}.");
        if (vm.Credit < 0) ModelState.AddModelError("", "Credit cannot be negative.");

        if (!ModelState.IsValid)
        {
            await LoadLists();
            return View(vm);
        }

        batch!.QuantityLeft -= vm.Tablets;
        _db.PurchaseReturns.Add(new PurchaseReturn
        {
            SupplierId = vm.SupplierId,
            CreditTotal = vm.Credit,
            Reason = string.IsNullOrWhiteSpace(vm.Reason) ? null : vm.Reason.Trim(),
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Items =
            {
                new PurchaseReturnItem
                {
                    BatchId = batch.Id,
                    TabletsReturned = vm.Tablets,
                    CreditAmount = vm.Credit
                }
            }
        });
        await _db.SaveChangesAsync();

        TempData["Ok"] = "Supplier return saved.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLists()
    {
        ViewBag.Suppliers = new SelectList(
            await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(),
            "Id", "Name");

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