using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface ICronScheduler
{
    Task<string> BackupSchedule();
    Task ClearAllJobsAsync();
    Task ScheduleAsync(CronJob job);
}

internal class CronJob
{
    public string Schedule { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}

internal class CronScheduler(ILogger<CronScheduler> logger, IFileService file, ISpecialPaths specialPaths) : ICronScheduler
{
    protected readonly ILogger<CronScheduler> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IFileService File = file ?? throw new ArgumentNullException(nameof(file));
    protected readonly ISpecialPaths SpecialPaths = specialPaths ?? throw new ArgumentNullException(nameof(specialPaths));

    public async Task<string> BackupSchedule()
    {
        // Use config value to set how many backups to keep and purge the rest

        var crontabFile = await this.SpecialPaths.GetCrontabFilePathAsync();
        var backupFile = $"{crontabFile}.{DateTime.Now:yyyyMMddHHmmss}.bak";

        await this.File.CopyAsync(crontabFile, backupFile);
        this.Logger.LogInformation("Crontab file backed up to {BackupFile}", backupFile);

        return backupFile;
    }

    public async Task ClearAllJobsAsync()
    {
        var crontabFile = await this.SpecialPaths.GetCrontabFilePathAsync();

        await File.WriteAllTextAsync(crontabFile, await this.GetCrontabFileTemplate());
    }

    public async Task ScheduleAsync(CronJob job)
    {
        var crontabFile = await this.SpecialPaths.GetCrontabFilePathAsync();

        await File.AppendAllTextAsync(crontabFile, $"{job.Schedule} {job.Command}");
    }

    protected virtual async Task<string> GetCrontabFileTemplate()
    {
        var assembly = typeof(CronScheduler).Assembly;
        using var stream = assembly.GetManifestResourceStream("CronTemplate") ?? throw new Exception("Cron template not found.");
        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var quackjobPath = await this.SpecialPaths.GetQuackJobExecutablePathAsync();
        var jobsDir = Path.Combine(Environment.GetEnvironmentVariable("QUACKVIEW_DIR")
            ?? throw new InvalidOperationException("QUACKVIEW_DIR environment variable is not set."), "jobs");

        return template.Replace("${timestamp}", timestamp, StringComparison.OrdinalIgnoreCase)
            .Replace("${quackjob}", quackjobPath, StringComparison.OrdinalIgnoreCase)
            .Replace("${jobs_dir}", jobsDir, StringComparison.OrdinalIgnoreCase);
    }
}