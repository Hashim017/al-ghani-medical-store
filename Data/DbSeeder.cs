using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AlGhaniMedicalStore.Models;

namespace AlGhaniMedicalStore.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Owner", "Staff" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.FindByNameAsync("owner") == null)
        {
            var owner = new AppUser
            {
                UserName = "owner",
                Email = "owner@shop.local",
                FullName = "Owner",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(owner, "Owner@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(owner, "Owner");
        }

        if (!await db.Settings.AnyAsync())
        {
            db.Settings.Add(new Setting { ShopName = "Al-Ghani Medical Store" });
            await db.SaveChangesAsync();
        }
    }
}