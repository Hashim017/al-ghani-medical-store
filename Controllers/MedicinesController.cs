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
    [Bind("Name,Form,RackLocation,ReorderLevel,UsesStrips,StripsPerBox,TabletsPerStrip,BoxPrice,StripPrice,TabletPrice,CategoryId,CompanyId,GenericId")] Medicine m,
    string? next)
    {
        Prepare(m);
        m.Name = (m.Name ?? "").Trim();
        if (await _db.Medicines.AnyAsync(x => x.Name == m.Name && x.Id != m.Id))
            ModelState.AddModelError(nameof(m.Name), "This medicine already exists.");
        m.Name = (m.Name ?? "").Trim();

        if (await _db.Medicines.AnyAsync(x => x.Name == m.Name))
            ModelState.AddModelError(nameof(m.Name), "This medicine already exists.");

        if (!ModelState.IsValid)
        {
            await LoadLists();
            return View(m);
        }

        m.IsActive = true;
        _db.Medicines.Add(m);
        await _db.SaveChangesAsync();

        if (next == "stock")
            return RedirectToAction("Create", "OpeningStock", new { medicineId = m.Id });

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
                [Bind("Id,Name,Form,RackLocation,ReorderLevel,IsActive,UsesStrips,StripsPerBox,TabletsPerStrip,BoxPrice,StripPrice,TabletPrice,CategoryId,CompanyId,GenericId")] Medicine m)
    {
        if (id != m.Id) return BadRequest();
        Prepare(m);
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await _db.Medicines.FindAsync(id);
        if (m == null) return RedirectToAction(nameof(Index));

        if (await _db.Batches.AnyAsync(b => b.MedicineId == id))
        {
            TempData["Err"] = $"{m.Name} has purchase history. Turn it off instead.";
            return RedirectToAction(nameof(Index));
        }

        _db.Medicines.Remove(m);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Medicine deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate(string? name, string? form, bool usesStrips, int stripsPerBox, int tabletsPerStrip)
    {
        name = (name ?? "").Trim();
        if (name == "")
            return BadRequest(new { error = "Name is required." });
        if (await _db.Medicines.AnyAsync(x => x.Name == name))
            return BadRequest(new { error = "This medicine already exists." });

        var m = new Medicine
        {
            Name = name,
            Form = string.IsNullOrWhiteSpace(form) ? null : form,
            UsesStrips = usesStrips,
            StripsPerBox = stripsPerBox < 1 ? 1 : stripsPerBox,
            TabletsPerStrip = usesStrips ? (tabletsPerStrip < 1 ? 1 : tabletsPerStrip) : 1
        };
        _db.Medicines.Add(m);
        await _db.SaveChangesAsync();

        return Json(new
        {
            id = m.Id,
            name = m.Name,
            usesStrips = m.UsesStrips,
            boxPrice = 0,
            stripPrice = 0,
            tabletPrice = 0
        });
    }

    private void Prepare(Medicine m)
    {
        // Empty boxes are allowed. They keep the default value.
        foreach (var key in new[] { "StripsPerBox", "TabletsPerStrip", "BoxPrice", "StripPrice", "TabletPrice", "ReorderLevel" })
            ModelState.Remove(key);

        if (m.StripsPerBox < 1) m.StripsPerBox = 1;
        if (m.TabletsPerStrip < 1) m.TabletsPerStrip = 1;

        if (!m.UsesStrips)
        {
            // Syrups and cosmetics are counted in pieces
            m.TabletsPerStrip = 1;
            m.StripPrice = m.TabletPrice;
        }

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