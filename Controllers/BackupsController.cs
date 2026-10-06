using AlGhaniMedicalStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlGhaniMedicalStore.Controllers;

public class BackupsController : Controller
{
    private readonly BackupService _backup;
    public BackupsController(BackupService backup) => _backup = backup;

    public IActionResult Index()
    {
        ViewBag.Folder = _backup.Folder;
        ViewBag.Error = _backup.LastError;

        var files = Directory.Exists(_backup.Folder)
            ? Directory.GetFiles(_backup.Folder, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList()
            : new List<FileInfo>();

        return View(files);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Run()
    {
        var file = await _backup.RunAsync();
        if (file != null)
            TempData["Ok"] = "Backup saved.";
        return RedirectToAction(nameof(Index));
    }
}