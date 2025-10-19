using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IAlertService
{
    Task AddAlertAsync(Alert alert);

    Task ClearAlertsAsync();

    Task ClearExpiredAlertsAsync();

    IEnumerable<Alert> GetAlerts();
}

internal class AlertService(IDataFileService dataFileService) : IAlertService
{
    public const string AlertsFile = "alerts.json";
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));

    public async Task AddAlertAsync(Alert alert)
    {
        ArgumentNullException.ThrowIfNull(alert, nameof(alert));

        await this.dataFileService.AppendToJsonListFileAsync<Alert>(AlertService.AlertsFile, alert);
    }

    public Task ClearAlertsAsync()
    {
        throw new NotImplementedException();
    }

    public Task ClearExpiredAlertsAsync()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Alert> GetAlerts()
    {
        throw new NotImplementedException();
    }
}