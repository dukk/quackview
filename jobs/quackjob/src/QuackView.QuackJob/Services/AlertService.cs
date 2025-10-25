using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IAlertService
{
    Task AddAlertAsync(Alert alert);

    Task ClearAlertsAsync();

    Task ClearExpiredAlertsAsync();

    Task<IEnumerable<Alert>> GetAlertsAsync();
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

    public async Task ClearAlertsAsync()
    {
        await this.dataFileService.DeleteAllJsonListItemsAsync<Alert>(AlertService.AlertsFile);
    }

    public async Task ClearExpiredAlertsAsync()
    {
        await this.dataFileService.DeleteJsonListItemsAsync<Alert>(AlertService.AlertsFile,
            alert => alert.Expires < DateTime.UtcNow);
    }

    public async Task<IEnumerable<Alert>> GetAlertsAsync()
    {
        var alertsFile = await this.dataFileService.ReadJsonListFileAsync<Alert>(AlertService.AlertsFile);

        return alertsFile.List;
    }
}