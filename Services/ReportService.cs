using AlGhaniMedicalStore.Data;
using AlGhaniMedicalStore.Models;
using AlGhaniMedicalStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) => _db = db;

    public async Task<int> GetAlertDaysAsync()
    {
        var d = await _db.Settings.Select(s => s.ExpiryAlertDays).FirstOrDefaultAsync();
        return d > 0 ? d : 60;
    }

    public async Task<DailyReportVm> GetDailyAsync(DateTime date)
    {
        var from = date.Date;
        var to = from.AddDays(1);

        var sales = _db.Sales.Where(s => s.SaleDate >= from && s.SaleDate < to);
        var purchases = _db.Purchases.Where(p => p.PurchaseDate >= from && p.PurchaseDate < to);

        var vm = new DailyReportVm { Date = from };

        vm.Bills = await sales.CountAsync();
        vm.SubTotal = await sales.SumAsync(s => (decimal?)s.SubTotal) ?? 0;
        vm.Discount = await sales.SumAsync(s => (decimal?)s.Discount) ?? 0;
        vm.SalesTotal = await sales.SumAsync(s => (decimal?)s.Total) ?? 0;
        vm.Cash = await sales.Where(s => s.PaymentType == PaymentType.Cash)
            .SumAsync(s => (decimal?)s.Total) ?? 0;
        vm.Online = await sales.Where(s => s.PaymentType == PaymentType.Online)
            .SumAsync(s => (decimal?)s.Total) ?? 0;

        var gross = await _db.SaleItems
            .Where(i => i.Sale!.SaleDate >= from && i.Sale.SaleDate < to)
            .SumAsync(i => (decimal?)(i.LineTotal - i.CostPerTablet * i.TabletsDeducted)) ?? 0;
        vm.Profit = gross - vm.Discount;

        vm.PurchaseCash = await purchases.Where(p => p.PaymentType == PaymentType.Cash)
            .SumAsync(p => (decimal?)p.Total) ?? 0;
        vm.PurchaseOnline = await purchases.Where(p => p.PaymentType == PaymentType.Online)
            .SumAsync(p => (decimal?)p.Total) ?? 0;

        vm.Top = await _db.SaleItems
            .Where(i => i.Sale!.SaleDate >= from && i.Sale.SaleDate < to)
            .GroupBy(i => i.Batch!.Medicine!.Name)
            .Select(g => new TopItem
            {
                Name = g.Key,
                Tablets = g.Sum(x => x.TabletsDeducted),
                Amount = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Amount)
            .Take(10)
            .ToListAsync();

        return vm;
    }

    public async Task<List<ExpiryRow>> GetExpiringAsync(int days)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var limit = today.AddDays(days);

        var rows = await _db.Batches
            .Where(b => b.QuantityLeft > 0 && b.ExpiryDate > today && b.ExpiryDate <= limit)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new ExpiryRow
            {
                MedicineName = b.Medicine!.Name,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate,
                QuantityLeft = b.QuantityLeft,
                CostValue = b.QuantityLeft * b.CostPricePerTablet
            })
            .ToListAsync();

        foreach (var r in rows)
            r.DaysLeft = r.ExpiryDate.DayNumber - today.DayNumber;

        return rows;
    }

    public async Task<List<ExpiryRow>> GetExpiredAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rows = await _db.Batches
            .Where(b => b.QuantityLeft > 0 && b.ExpiryDate <= today)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new ExpiryRow
            {
                MedicineName = b.Medicine!.Name,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate,
                QuantityLeft = b.QuantityLeft,
                CostValue = b.QuantityLeft * b.CostPricePerTablet
            })
            .ToListAsync();

        foreach (var r in rows)
            r.DaysLeft = r.ExpiryDate.DayNumber - today.DayNumber;

        return rows;
    }

    public async Task<List<DayPoint>> GetWeekAsync()
    {
        var start = DateTime.Today.AddDays(-6);

        var rows = await _db.Sales
            .Where(s => s.SaleDate >= start)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new { Day = g.Key, Total = g.Sum(x => x.Total) })
            .ToListAsync();

        var list = new List<DayPoint>();
        for (int i = 0; i < 7; i++)
        {
            var d = start.AddDays(i);
            list.Add(new DayPoint
            {
                Label = d.ToString("ddd"),
                Total = rows.FirstOrDefault(r => r.Day == d)?.Total ?? 0
            });
        }
        return list;
    }

    public async Task<List<LowStockRow>> GetLowStockAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await _db.Medicines
            .Where(m => m.IsActive && m.ReorderLevel > 0)
            .Select(m => new LowStockRow
            {
                Name = m.Name,
                Rack = m.RackLocation,
                ReorderLevel = m.ReorderLevel,
                Stock = m.Batches
                    .Where(b => b.ExpiryDate > today)
                    .Sum(b => (int?)b.QuantityLeft) ?? 0
            })
            .Where(x => x.Stock <= x.ReorderLevel)
            .OrderBy(x => x.Stock)
            .ToListAsync();
    }
}