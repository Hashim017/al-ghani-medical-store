using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class MedicinesController : Controller
{
    private readonly AppDbContext _db;
    public MedicinesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Medicines
            .Include(m => m.Company)
            .Include(m => m.Generic)
            .Include(m => m.Batches)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(m => m.Name.Contains(q)
                || (m.Generic != null && m.Generic.Name.Contains(q)));
        }

        ViewBag.Q = q;
        var list = await query.OrderBy(m => m.Name).Take(200).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLists();
        return View(new Medicine());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Form,RackLocation,ReorderLevel,StripsPerBox,TabletsPerStrip,BoxPrice,StripPrice,TabletPrice,CategoryId,CompanyId,GenericId")] Medicine m)
    {
        CheckValues(m);
        if (!ModelState.IsValid)
        {
            await LoadLists();
            return View(m);
        }
        m.IsActive = true;
        _db.Medicines.Add(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var m = await _db.Medicines.FindAsync(id);
        if (m == null) return NotFound();
        await LoadLists();
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,Name,Form,RackLocation,ReorderLevel,IsActive,StripsPerBox,TabletsPerStrip,BoxPrice,StripPrice,TabletPrice,CategoryId,CompanyId,GenericId")] Medicine m)
    {
        if (id != m.Id) return BadRequest();
        CheckValues(m);
        if (!ModelState.IsValid)
        {
            await LoadLists();
            return View(m);
        }
        _db.Update(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var m = await _db.Medicines.FindAsync(id);
        if (m != null)
        {
            m.IsActive = !m.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private void CheckValues(Medicine m)
    {
        if (m.StripsPerBox < 1)
            ModelState.AddModelError(nameof(m.StripsPerBox), "Must be at least 1.");
        if (m.TabletsPerStrip < 1)
            ModelState.AddModelError(nameof(m.TabletsPerStrip), "Must be at least 1.");
        if (m.ReorderLevel < 0)
            ModelState.AddModelError(nameof(m.ReorderLevel), "Cannot be negative.");
        if (m.BoxPrice < 0 || m.StripPrice < 0 || m.TabletPrice < 0)
            ModelState.AddModelError("", "Prices cannot be negative.");
    }

    private async Task LoadLists()
    {
        ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Companies = new SelectList(await _db.Companies.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Generics = new SelectList(await _db.Generics.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
    }
}