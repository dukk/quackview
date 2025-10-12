using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace TypoDukk.Dashboard.GraphJobs.Services;

internal class CalendarEvent
{
    public string? Subject { get; internal set; }
    public string? Location { get; internal set; }
    public string? Start { get; internal set; }
    public string? End { get; internal set; }
    public string? BodyPreview { get; internal set; }
    public bool IsAllDay { get; internal set; }
    public string? Calendar { get; internal set; }
    public string? Account { get; internal set; }
}

internal interface ICalendarEventService
{
    Task<IEnumerable<CalendarEventCalendar>> GetCalendarsAsync(string accountName);

    Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountName, string calendarName, DateTime start, DateTime end);

    Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountName, string[] calendarNames, DateTime start, DateTime end);
}

internal class CalendarEventCalendar
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

internal class CalendarEventService : ICalendarEventService
{
    private readonly ILogger<CalendarEventService> logger;
    private readonly IGraphService graphService;

    public CalendarEventService(ILogger<CalendarEventService> logger, IGraphService graphService)
    {
        this.logger = logger;
        this.graphService = graphService;
    }

    public async Task<IEnumerable<CalendarEventCalendar>> GetCalendarsAsync(string accountName)
    {
        var calendars = await this.graphService.ExecuteInContextAsync(async (client) =>
        {
            var calendarsResponse = await client.Me.Calendars.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Select = ["id", "name"];
            });

            if (calendarsResponse?.Value == null)
                return null;

            return calendarsResponse?.Value?.Select(c => new CalendarEventCalendar
            {
                Id = c.Id,
                Name = c.Name ?? string.Empty
            });
        }, accountName);

        return calendars ?? [];
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountName, string[] calendarNames, DateTime start, DateTime end)
    {
        logger.LogInformation("Fetching calendar events from {start} to {end} for calendars: {calendarNames} on account {accountName}", start, end, string.Join(", ", calendarNames), accountName);

        var calendars = await this.GetCalendarsAsync(accountName);
        var allEvents = new List<CalendarEvent>();

        if (!calendars.Any(c => calendarNames.Contains(c.Name)))
            logger.LogWarning("No calendars found on account: {accountName} matching one or more of the specified calendar names: {calendarNames}", accountName, string.Join(", ", calendarNames));

        foreach (var calendar in calendars.Where(c => calendarNames.Contains(c.Name)))
        {
            allEvents.AddRange(await this.graphService.ExecuteInContextAsync(async (client) =>
            {
                var eventsResponse = await client.Me.Calendars[calendar.Id].Events.GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Filter = $"start/dateTime ge '{start.ToString("o")}' and end/dateTime le '{end.ToString("o")}'";
                    requestConfig.Headers.Add("Prefer", "outlook.timezone=UTC");
                    requestConfig.QueryParameters.Select = ["subject", "location", "start", "end", "isAllDay"];
                });

                if (eventsResponse?.Value == null)
                    return Enumerable.Empty<CalendarEvent>();

                this.logger.LogInformation("Found {EventCount} events in calendar '{CalendarName}'", eventsResponse.Value.Count, calendar.Name);

                return eventsResponse.Value.Select(eventResponse => new CalendarEvent
                    {
                        Subject = eventResponse.Subject,
                        Location = eventResponse.Location?.DisplayName,
                        Start = eventResponse.Start?.DateTime,
                        End = eventResponse.End?.DateTime,
                        IsAllDay = eventResponse.IsAllDay ?? false,
                        BodyPreview = eventResponse.BodyPreview,
                        Calendar = calendar.Name,
                        Account = accountName
                    }).ToList();
            }, accountName));
        }
            
        return allEvents.ToImmutableArray();
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsAsync(string accountName, string calendarName, DateTime start, DateTime end)
    {
        return await GetEventsAsync(accountName, [calendarName], start, end);
    }
}   