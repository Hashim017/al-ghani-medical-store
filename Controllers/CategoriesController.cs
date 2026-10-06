using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var items = await _db.Categories.OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
        return View("~/Views/Shared/Lookup.cshtml",
            new LookupVm { Title = "Categories", ControllerName = "Categories", Items = items });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        name = (name ?? "").Trim();
        if (name == "")
            TempData["Error"] = "Name is required.";
        else if (await _db.Categories.AnyAsync(x => x.Name == name))
            TempData["Error"] = "This name already exists.";
        else
        {
            _db.Categories.Add(new Category { Name = name });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Categories.FindAsync(id);
        if (item != null)
        {
            if (await _db.Medicines.AnyAsync(m => m.CategoryId == id))
                TempData["Error"] = "Some medicines use this category. You cannot delete it.";
            else
            {
                _db.Categories.Remove(item);
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Index));
    }
}