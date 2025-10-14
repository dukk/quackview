using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using TypoDukk.QuackView.QuackJob.Data.Shared;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class UpcomingCalendarEventsJob(
    ILogger<UpcomingCalendarEventsJob> logger,
    IOutlookCalendarEventService outlookCalendarEventService,
    IDataFileService dataFileService) : Job<UpcomingCalendarEventsJobConfig>
{
    private readonly ILogger<UpcomingCalendarEventsJob> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOutlookCalendarEventService outlookCalendarEventService = outlookCalendarEventService ?? throw new ArgumentNullException(nameof(outlookCalendarEventService));
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));

    public override async Task ExecuteAsync(UpcomingCalendarEventsJobConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Accounts.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(config), "Invalid job configuration.");

        logger.LogInformation("Executing Upcoming Calendar Events job.");
        
        var allEvents = new List<CalendarEvent>();

        foreach (var account in config.Accounts)
        {
            allEvents.AddRange(await this.outlookCalendarEventService.GetEventsAsync(
                account.Account,
                account.Calendars,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(config.DaysInFuture)));
        }

        await this.dataFileService.WriteJsonFile(config.OutputFileName, allEvents.OrderBy(e => e.Start));

        return;
    }
}

public class UpcomingCalendarEventsJobConfig()
{   
    public IList<CalendarAccounts> Accounts { get; set; } = [];
    public int DaysInFuture { get; set; } = 14;

    public string OutputFileName { get; set; } = "calendar-events.json";
}

public class CalendarAccounts()
{
    public string[] Calendars { get; set; } = [];
    public string Account { get; set; } = string.Empty;
}