using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class RebuildScheduleAction(ILogger<RebuildScheduleAction> logger, ICronScheduler cronScheduler, IConsoleService console, IDirectoryService directory) : Action(console)
{
    private readonly ILogger<RebuildScheduleAction> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));
    private readonly ICronScheduler cronScheduler = cronScheduler ?? throw new ArgumentNullException(nameof(cronScheduler));

    public async override Task ExecuteAsync(string[] args)
    {
        var jobsDir = Path.Combine(Environment.GetEnvironmentVariable("QUACKVIEW_DIR")
            ?? throw new InvalidOperationException("QUACKVIEW_DIR environment variable is not set."), "jobs");

        if (string.IsNullOrWhiteSpace(jobsDir) || !await directory.ExistsAsync(jobsDir))
            throw new DirectoryNotFoundException("Jobs directory not found.");

        await this.cronScheduler.ClearAllJobsAsync();

        var quackjobPath = this.GetQuackjobExecutablePath();

        foreach (var file in await directory.EnumerateFilesAsync(jobsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var jobFile = JsonSerializer.Deserialize<JobFile<object>>(json);

                if (jobFile == null || string.IsNullOrWhiteSpace(jobFile.Metadata.Schedule))
                {
                    this.logger.LogWarning("Invalid or missing schedule in job file: {File}", file);
                    continue;
                }

                await this.cronScheduler.ScheduleAsync(new CronJob()
                {
                    Schedule = jobFile.Metadata.Schedule,
                    Command = $"{quackjobPath} run {jobFile.Metadata.Runner} --config={file}"
                });

                this.logger.LogInformation("Scheduled job {Name} from {File} with schedule {Schedule}",
                    jobFile.Metadata.Name, file, jobFile.Metadata.Schedule);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to schedule job from file: {File}", file);
            }
        }
    }
    
    protected virtual string GetQuackjobExecutablePath()
    {
        return Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the path of the current executable.");
    }
}