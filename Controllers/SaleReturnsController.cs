using System.Security.Claims;
using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class SaleReturnsController : Controller
{
    private readonly AppDbContext _db;
    public SaleReturnsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.SaleReturns
            .Include(r => r.Items)
            .OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.Id)
            .Take(100)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create(int? saleId)
    {
        if (saleId == null) return View(new SaleReturnVm());

        var vm = await Build(saleId.Value);
        if (vm == null)
        {
            TempData["Error"] = "Bill not found.";
            return RedirectToAction(nameof(Create));
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleReturnVm post)
    {
        var sale = await _db.Sales
            .Include(s => s.Items).ThenInclude(i => i.Batch)
            .FirstOrDefaultAsync(s => s.Id == post.SaleId);
        if (sale == null) return NotFound();

        var returned = await ReturnedTablets(sale.Id);
        var share = sale.SubTotal > 0 ? sale.Discount / sale.SubTotal : 0m;

        // First pass: check everything. Nothing is changed yet.
        var todo = new List<(SaleItem Item, int Qty, int PerUnit)>();
        foreach (var line in post.Lines.Where(l => l.ReturnQty > 0))
        {
            var item = sale.Items.FirstOrDefault(i => i.Id == line.SaleItemId);
            if (item == null) continue;

            int perUnit = item.TabletsDeducted / item.Quantity;
            int left = item.Quantity - returned.GetValueOrDefault(item.Id) / perUnit;

            if (line.ReturnQty > left)
                ModelState.AddModelError("", $"{item.Batch?.BatchNumber}: only {left} can still be returned.");
            else
                todo.Add((item, line.ReturnQty, perUnit));
        }

        if (todo.Count == 0 && ModelState.IsValid)
            ModelState.AddModelError("", "Enter a return quantity for at least one line.");

        if (!ModelState.IsValid)
        {
            var again = await Build(sale.Id);
            foreach (var l in again!.Lines)
                l.ReturnQty = post.Lines.FirstOrDefault(p => p.SaleItemId == l.SaleItemId)?.ReturnQty ?? 0;
            again.Reason = post.Reason;
            return View(again);
        }

        // Second pass: save
        var ret = new SaleReturn
        {
            SaleId = sale.Id,
            Reason = string.IsNullOrWhiteSpace(post.Reason) ? null : post.Reason.Trim(),
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        foreach (var (item, qty, perUnit) in todo)
        {
            item.Batch!.QuantityLeft += qty * perUnit;
            var unitRefund = Math.Round(item.UnitPrice * (1 - share), 2);
            ret.Items.Add(new SaleReturnItem
            {
                SaleItemId = item.Id,
                TabletsReturned = qty * perUnit,
                RefundAmount = Math.Round(qty * unitRefund, 2)
            });
        }

        ret.RefundTotal = ret.Items.Sum(i => i.RefundAmount);
        _db.SaleReturns.Add(ret);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Return saved. Refund {ret.RefundTotal:N2}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Dictionary<int, int>> ReturnedTablets(int saleId)
    {
        return await _db.SaleReturnItems
            .Where(r => r.SaleReturn!.SaleId == saleId)
            .GroupBy(r => r.SaleItemId)
            .Select(g => new { Id = g.Key, Tablets = g.Sum(x => x.TabletsReturned) })
            .ToDictionaryAsync(x => x.Id, x => x.Tablets);
    }

    private async Task<SaleReturnVm?> Build(int saleId)
    {
        var sale = await _db.Sales
            .Include(s => s.Items).ThenInclude(i => i.Batch).ThenInclude(b => b!.Medicine)
            .FirstOrDefaultAsync(s => s.Id == saleId);
        if (sale == null) return null;

        var returned = await ReturnedTablets(saleId);
        var share = sale.SubTotal > 0 ? sale.Discount / sale.SubTotal : 0m;

        var vm = new SaleReturnVm { SaleId = sale.Id, SaleDate = sale.SaleDate, Total = sale.Total };
        foreach (var i in sale.Items)
        {
            int perUnit = i.TabletsDeducted / i.Quantity;
            vm.Lines.Add(new SaleReturnLine
            {
                SaleItemId = i.Id,
                Medicine = i.Batch?.Medicine?.Name ?? "",
                Batch = i.Batch?.BatchNumber ?? "",
                Unit = i.UnitSold,
                SoldQty = i.Quantity,
                ReturnedQty = returned.GetValueOrDefault(i.Id) / perUnit,
                UnitRefund = Math.Round(i.UnitPrice * (1 - share), 2)
            });
        }
        return vm;
    }
}