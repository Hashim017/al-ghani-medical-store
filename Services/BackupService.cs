using AlGhaniMedicalStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AlGhaniMedicalStore.Services;

public class BackupService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _cfg;

    public BackupService(IServiceScopeFactory scopes, IConfiguration cfg)
    {
        _scopes = scopes;
        _cfg = cfg;
    }

    public string? LastError { get; private set; }
    public string Folder => _cfg["Backup:Folder"] ?? @"C:\AlGhaniBackups";
    private string? CopyTo => string.IsNullOrWhiteSpace(_cfg["Backup:CopyTo"]) ? null : _cfg["Backup:CopyTo"];
    private int Keep => int.TryParse(_cfg["Backup:Keep"], out var k) && k > 0 ? k : 14;

    public bool HasBackupToday()
    {
        if (!Directory.Exists(Folder)) return false;
        var tag = "_" + DateTime.Now.ToString("yyyy-MM-dd") + "_";
        return Directory.GetFiles(Folder, "*.bak").Any(f => Path.GetFileName(f).Contains(tag));
    }

    public async Task<string?> RunAsync()
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Directory.CreateDirectory(Folder);
            var dbName = db.Database.GetDbConnection().Database;
            var file = Path.Combine(Folder, $"{dbName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.bak");

            await db.Database.ExecuteSqlRawAsync(
                "BACKUP DATABASE [" + dbName + "] TO DISK = {0} WITH INIT", file);

            if (CopyTo != null)
            {
                Directory.CreateDirectory(CopyTo);
                File.Copy(file, Path.Combine(CopyTo, Path.GetFileName(file)), true);
            }

            Cleanup(Folder);
            if (CopyTo != null) Cleanup(CopyTo);

            LastError = null;
            return file;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    private void Cleanup(string folder)
    {
        if (!Directory.Exists(folder)) return;
        var old = Directory.GetFiles(folder, "*.bak")
            .OrderByDescending(f => File.GetCreationTime(f))
            .Skip(Keep);
        foreach (var f in old)
        {
            try { File.Delete(f); } catch { }
        }
    }
}