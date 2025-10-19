using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using Microsoft.IdentityModel.Tokens;
using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class UpcomingCalendarEventsJob(
    ILogger<UpcomingCalendarEventsJob> logger,
    IOutlookCalendarEventService outlookCalendarEventService,
    IDataFileService dataFileService) : JobRunner
{
    private readonly ILogger<UpcomingCalendarEventsJob> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOutlookCalendarEventService outlookCalendarEventService = outlookCalendarEventService ?? throw new ArgumentNullException(nameof(outlookCalendarEventService));
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));

    public override async Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        var config = this.LoadJsonConfig<UpcomingCalendarEventsJobConfig>(jsonConfig)
            ?? throw new ArgumentException("Invalid job configuration.", nameof(jsonConfig));

        if (config.Accounts.IsNullOrEmpty())
            throw new ArgumentException("Invalid job configuration. No accounts specified.", nameof(config));

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

        await this.dataFileService.WriteJsonFileAsync(config.OutputFileName, allEvents.OrderBy(e => e.Start));

        return;
    }
}

public class UpcomingCalendarEventsJobConfig()
{   
    public IList<CalendarAccounts> Accounts { get; set; } = [];
    public int DaysInFuture { get; set; } = 14;

    public string OutputFileName { get; set; } = "calendar/upcoming-events.json";
}

public class CalendarAccounts()
{
    public string[] Calendars { get; set; } = [];
    public string Account { get; set; } = string.Empty;
}