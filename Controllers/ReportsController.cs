using AlGhaniMedicalStore.Services;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AlGhaniMedicalStore.Controllers;

public class ReportsController : Controller
{
    private readonly ReportService _reports;
    public ReportsController(ReportService reports) => _reports = reports;

    public async Task<IActionResult> Daily(DateTime? date)
    {
        var vm = await _reports.GetDailyAsync(date ?? DateTime.Today);
        return View(vm);
    }

    public async Task<IActionResult> Expiry(int? days)
    {
        var d = days ?? await _reports.GetAlertDaysAsync();
        if (d < 1) d = 1;
        if (d > 730) d = 730;

        var vm = new ExpiryPageVm
        {
            Days = d,
            Expired = await _reports.GetExpiredAsync(),
            Expiring = await _reports.GetExpiringAsync(d)
        };
        return View(vm);
    }

    public async Task<IActionResult> LowStock()
    {
        return View(await _reports.GetLowStockAsync());
    }
}