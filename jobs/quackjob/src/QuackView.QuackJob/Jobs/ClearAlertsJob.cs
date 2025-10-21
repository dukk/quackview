using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;
using System.Text.Json;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class ClearExpiredAlertsJob(IAlertService alertService, ILogger<ClearExpiredAlertsJob> logger, IConsoleService console) : JobRunner
{
    protected readonly IAlertService AlertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
    protected readonly ILogger<ClearExpiredAlertsJob> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));

    public override async Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        this.Console.WriteLine("Clearing expired alerts.");
        await this.AlertService.ClearExpiredAlertsAsync();
    }
}
