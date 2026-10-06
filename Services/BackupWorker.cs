namespace AlGhaniMedicalStore.Services;

public class BackupWorker : BackgroundService
{
    private readonly BackupService _backup;
    public BackupWorker(BackupService backup) => _backup = backup;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app start first
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_backup.HasBackupToday())
                await _backup.RunAsync();

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}