using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;
using System.Text.Json;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class ClearExpiredAlertsJob(IAlertService alertService, ILogger<ClearExpiredAlertsJob> logger) : JobRunner
{
    private readonly IAlertService alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
    private readonly ILogger<ClearExpiredAlertsJob> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override async Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        logger.LogInformation("Executing clear expired alerts.");

        await this.alertService.ClearExpiredAlertsAsync();
    }
}
