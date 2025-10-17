namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IAlertService
{
    void SendAlert(string message);
}

internal class AlertService(IDataFileService dataFileService) : IAlertService
{
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));

    public void SendAlert(string message)
    {
        
    }
}