using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Controllers;

public class GenericsController : Controller
{
    private readonly AppDbContext _db;
    public GenericsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var items = await _db.Generics.OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.Id, x.Name)).ToListAsync();
        return View("~/Views/Shared/Lookup.cshtml",
            new LookupVm { Title = "Generics (Salts)", ControllerName = "Generics", Items = items });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        name = (name ?? "").Trim();
        if (name == "")
            TempData["Error"] = "Name is required.";
        else if (await _db.Generics.AnyAsync(x => x.Name == name))
            TempData["Error"] = "This name already exists.";
        else
        {
            _db.Generics.Add(new Generic { Name = name });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Generics.FindAsync(id);
        if (item != null)
        {
            if (await _db.Medicines.AnyAsync(m => m.GenericId == id))
                TempData["Error"] = "Some medicines use this generic. You cannot delete it.";
            else
            {
                _db.Generics.Remove(item);
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Index));
    }
}