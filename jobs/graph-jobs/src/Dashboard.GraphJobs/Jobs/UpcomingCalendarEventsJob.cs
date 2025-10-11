using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.Dashboard.GraphJobs.Services;

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
        logger.LogInformation("Starting CalendarEventsJob...");

        var events = await this.calendarEventService.GetEventsAsync(
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(config.DaysInFuture));

        await this.dataFileService.WriteJsonFile(config.OutputFileName, events);

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
    public string AccountUserName { get; set; } = string.Empty;
}