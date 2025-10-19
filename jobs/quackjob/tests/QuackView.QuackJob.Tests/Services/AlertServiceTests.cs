using NSubstitute;
using TypoDukk.QuackView.QuackJob.Data;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Tests.Services;

[TestClass]
internal sealed class AlertServiceTests
{
    [TestMethod]
    public async Task AddAlertAsync_ValidAlert_AppendsToFile()
    {
        // Arrange
        var dataFileService = Substitute.For<IDataFileService>();
        var alertService = new AlertService(dataFileService);
        var alert = this.GetTestAlert();

        // Act
        await alertService.AddAlertAsync(alert);

        // Assert
        await dataFileService.Received(1).AppendToJsonListFileAsync<Alert>(AlertService.AlertsFile, alert);
    }

    public Alert GetTestAlert()
    {
        return new Alert
        {
            Title = "Sample Alert",
            Message = "This is a sample alert for testing.",
            Effective = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddHours(2)
        };
    }

    public string GetTestAlertJson()
    {
        return @"{
            ""title"": ""Sample Alert"",
            ""message"": ""This is a sample alert for testing."",
            ""effective"": ""2024-01-01T12:00:00Z"",
            ""expires"": ""2024-01-01T14:00:00Z""
        }";
    }
}