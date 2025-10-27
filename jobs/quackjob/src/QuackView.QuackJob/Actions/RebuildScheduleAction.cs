using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

// [Action()]
internal class RebuildScheduleAction(
    ILogger<RebuildScheduleAction> logger,
    ICronScheduler cronScheduler,
    IConsoleService console,
    IDirectoryService directory,
    IFileService file,
    ISpecialPaths SpecialPaths)
    : Action(logger, console)
{
    protected readonly new ILogger<RebuildScheduleAction> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDirectoryService Directory = directory ?? throw new ArgumentNullException(nameof(directory));
    protected readonly ICronScheduler CronScheduler = cronScheduler ?? throw new ArgumentNullException(nameof(cronScheduler));
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));
    protected readonly IFileService File = file ?? throw new ArgumentNullException(nameof(file));

    public async override Task ExecuteAsync(string[] args)
    {
        var jobsDir = await this.SpecialPaths.GetJobsDirectoryPathAsync();
        var quackjobPath = await this.SpecialPaths.GetQuackJobExecutablePathAsync();

        if (string.IsNullOrWhiteSpace(jobsDir) || !await this.Directory.ExistsAsync(jobsDir))
            throw new DirectoryNotFoundException("Jobs directory not found.");

        var backupFile = await this.CronScheduler.BackupSchedule();

        this.Console.WriteLine($"Crontab backed up to: {backupFile}");
        // this.Console.WriteLine($"To revert to your backup use the command:");
        // this.Console.WriteLine($"\t{quackjobPath} restore-schedule --file='{backupFile}'");
        // this.Console.WriteLine($"Only the last {x} number of backups will be kept");

        await this.CronScheduler.ClearAllJobsAsync();

        foreach (var file in await this.Directory.EnumerateFilesAsync(jobsDir, "*.json"))
        {
            try
            {
                var json = await this.File.ReadAllTextAsync(file);
                var jobFile = JsonSerializer.Deserialize<JobFile>(json, Program.DefaultJsonSerializerOptions);

                if (jobFile == null || string.IsNullOrWhiteSpace(jobFile.Metadata.Schedule))
                {
                    this.Console.WriteError($"Invalid or missing schedule in job file: {file}");
                    continue;
                }

                await this.CronScheduler.ScheduleAsync(new CronJob()
                {
                    Schedule = jobFile.Metadata.Schedule,
                    Command = $"{quackjobPath} run \"{file}\""
                });

                this.Console.WriteLine($"Scheduled job {jobFile.Metadata.Name} from {file} with schedule {jobFile.Metadata.Schedule}");
            }
            catch (Exception ex)
            {
                this.Console.WriteError($"Failed to schedule job from file: {file} - {ex.Message}");
            }
        }
    }

    protected virtual string GetQuackjobExecutablePath()
    {
        return Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the path of the current executable.");
    }
}