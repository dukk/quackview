using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;
using System.Text.Json;

namespace TypoDukk.QuackView.QuackJob.Jobs;

[JobRunner("clear-expired-alerts", "Clears expired alerts")]
internal class ClearExpiredAlertsJobRunner(
    IAlertService alertService,
    ILogger<ClearExpiredAlertsJobRunner> logger,
    IConsoleService console,
    IDiskIOService file)
    : JobRunner(file)
{
    protected readonly IAlertService AlertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
    protected readonly ILogger<ClearExpiredAlertsJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));

    public override async Task ExecuteJobFileAsync(JobFile jobFile)
    {
        this.Console.WriteLine("Clearing expired alerts.");
        await this.AlertService.ClearExpiredAlertsAsync();
    }

    public override async Task CreateNewJobFileAsync(string filePath)
    {
        var content = JsonSerializer.Serialize(new JobFile()
        {
            Metadata = new()
            {
                Name = "Clear expired alerts",
                Description = "Clears expired alerts",
                Runner = "clear-expired-alerts",
                Schedule = "*/10 * * * *"
            }
        }, options: Program.DefaultJsonSerializerOptions);

        await this.Disk.AppendAllTextAsync(filePath, content);
    }
}
