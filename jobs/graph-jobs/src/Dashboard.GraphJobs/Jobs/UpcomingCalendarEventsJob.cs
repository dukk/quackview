using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.Dashboard.GraphJobs.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;

namespace TypoDukk.Dashboard.GraphJobs.Jobs;

internal class UpcomingCalendarEventsJob(
    ILogger<UpcomingCalendarEventsJob> logger,
    ICalendarEventService calendarEventService,
    IDataFileService dataFileService) : Job<UpcomingCalendarEventsJobConfig>
{
    private readonly ILogger<UpcomingCalendarEventsJob> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICalendarEventService calendarEventService = calendarEventService ?? throw new ArgumentNullException(nameof(calendarEventService));
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
            allEvents.AddRange(await this.calendarEventService.GetEventsAsync(
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