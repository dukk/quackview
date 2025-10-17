using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface ICronScheduler
{
    Task ClearAllJobs();
    Task Schedule(CronJob job);
}

internal class CronJob
{
    public string Schedule { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}

internal class CronScheduler(ILogger<CronScheduler> logger) : ICronScheduler
{
    private readonly ILogger<CronScheduler> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task ClearAllJobs()
    {
        var crontabFile = this.getCrontabFilePath();

        await File.WriteAllTextAsync(crontabFile, await this.getCrontabFileTemplate());
    }

    public async Task Schedule(CronJob job)
    {
        var crontabFile = this.getCrontabFilePath();

        await File.AppendAllLinesAsync(crontabFile, [$"{job.Schedule} {job.Command}"]);
    }

    private async Task<string> getCrontabFileTemplate()
    {
        var assembly = typeof(CronScheduler).Assembly;
        using var stream = assembly.GetManifestResourceStream("CronTemplate") ?? throw new Exception("Cron template not found.");
        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var quackjobsPath = this.getQuackJobExecutablePath();
        var jobsDir = Path.Combine(Environment.GetEnvironmentVariable("QUACKVIEW_DIR")
            ?? throw new InvalidOperationException("QUACKVIEW_DIR environment variable is not set."), "jobs");

        return template.Replace("${timestamp}", timestamp, StringComparison.OrdinalIgnoreCase)
            .Replace("${quackjobs}", quackjobsPath, StringComparison.OrdinalIgnoreCase)
            .Replace("${jobs_dir}", jobsDir, StringComparison.OrdinalIgnoreCase);
    }

    private string getQuackJobExecutablePath()
    {
        return Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the path of the current executable.");
    }

    private string getCrontabFilePath()
    {
        var path = Environment.GetEnvironmentVariable("QUACKVIEW_DIR");

        if (string.IsNullOrWhiteSpace(path))
            throw new Exception("Missing required QUACKVIEW_DIR environment variable.");

        if (!File.Exists(path))
            throw new InvalidOperationException("Unable to find crontab file (did you delete the symlink?)");

        return Path.Combine(path, "config", "crontab");
    }
}