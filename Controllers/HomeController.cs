using System.Diagnostics;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.Services;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AlGhaniMedicalStore.Controllers;

public class HomeController : Controller
{
    private readonly ReportService _reports;
    public HomeController(ReportService reports) => _reports = reports;

    public async Task<IActionResult> Index()
    {
        var days = await _reports.GetAlertDaysAsync();
        var expiring = await _reports.GetExpiringAsync(days);
        var expired = await _reports.GetExpiredAsync();
        var low = await _reports.GetLowStockAsync();

        var vm = new DashboardVm
        {
            AlertDays = days,
            Today = await _reports.GetDailyAsync(DateTime.Today),
            ExpiringCount = expiring.Count,
            ExpiredCount = expired.Count,
            LowStockCount = low.Count,
            Expiring = expiring.Take(8).ToList(),
            LowStock = low.Take(8).ToList()
        };
        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}