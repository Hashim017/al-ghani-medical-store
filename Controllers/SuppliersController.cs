using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class SuppliersController : Controller
{
    private readonly AppDbContext _db;
    public SuppliersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Suppliers.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name).ToListAsync();
        return View(list);
    }

    public IActionResult Create() => View(new Supplier());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Phone,Address")] Supplier s)
    {
        if (!ModelState.IsValid) return View(s);
        s.IsActive = true;
        _db.Suppliers.Add(s);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return NotFound();
        return View(s);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Address,IsActive")] Supplier s)
    {
        if (id != s.Id) return BadRequest();
        if (!ModelState.IsValid) return View(s);
        _db.Update(s);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s != null)
        {
            s.IsActive = !s.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s == null) return RedirectToAction(nameof(Index));

        if (await _db.Purchases.AnyAsync(p => p.SupplierId == id) ||
            await _db.PurchaseReturns.AnyAsync(r => r.SupplierId == id))
        {
            TempData["Err"] = $"{s.Name} has purchase history. Turn it off instead.";
            return RedirectToAction(nameof(Index));
        }

        _db.Suppliers.Remove(s);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Supplier deleted.";
        return RedirectToAction(nameof(Index));
    }
}