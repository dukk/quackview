using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class RebuildScheduleAction(ILogger<RebuildScheduleAction> logger, ICronScheduler cronScheduler, IConsoleService console, IDirectoryService directory) : Action(console)
{
    private readonly ILogger<RebuildScheduleAction> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));
    private readonly ICronScheduler cronScheduler = cronScheduler ?? throw new ArgumentNullException(nameof(cronScheduler));

    public override Task ExecuteAsync(string[] args)
    {
        var jobsDir = Path.Combine(Environment.GetEnvironmentVariable("QUACKVIEW_DIR")
            ?? throw new InvalidOperationException("QUACKVIEW_DIR environment variable is not set."), "jobs");

        if (string.IsNullOrWhiteSpace(jobsDir) || !directory.Exists(jobsDir))
            throw new DirectoryNotFoundException("Jobs directory not found.");

        this.cronScheduler.ClearAllJobs();

        var quackjobPath = this.getQuackjobsExecutablePath();

        foreach (var file in directory.EnumerateFiles(jobsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var jobFile = JsonSerializer.Deserialize<JobFile>(json);

                if (jobFile == null || string.IsNullOrWhiteSpace(jobFile.Job.Schedule))
                {
                    this.logger.LogWarning("Invalid or missing schedule in job file: {File}", file);
                    continue;
                }

                this.cronScheduler.Schedule(new CronJob()
                {
                    Schedule = jobFile.Job.Schedule,
                    Command = $"{quackjobPath} run {jobFile.Job.Type} --config={file}"
                });

                this.logger.LogInformation("Scheduled job {Name} from {File} with schedule {Schedule}",
                    jobFile.Job.Name, file, jobFile.Job.Schedule);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to schedule job from file: {File}", file);
            }
        }

        return Task.CompletedTask;
    }
    
    private string getQuackjobsExecutablePath()
    {
        return Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the path of the current executable.");
    }
}

internal class JobFile
{
    public JobMetadata Job { get; set; } = new JobMetadata();
    public JsonElement Config { get; set; }
}

internal class JobMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
}