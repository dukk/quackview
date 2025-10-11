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
}

internal interface ICalendarEventService
{
    Task<IEnumerable<string>> GetCalendarsAsync();
    Task<IEnumerable<CalendarEvent>> GetEventsAsync(DateTime start, DateTime end);
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

    public async Task<IEnumerable<string>> GetCalendarsAsync()
    {
        var calendars = await graphService.ExecuteInContextAsync(async (client) =>
        {
            var calendarsResponse = await client.Me.Calendars.GetAsync();
            return calendarsResponse?.Value?.Select(c => c.Name ?? "[Unnamed Calendar]") ?? Enumerable.Empty<string>();
        });

        this.logger.LogInformation("Fetched {CalendarCount} calendars", calendars.Count());

        return calendars;
    }

    public Task<IEnumerable<CalendarEvent>> GetEventsAsync(DateTime start, DateTime end)
    {
        return GetEventsAsync("Default", start, end);
    }

    public async Task<IEnumerable<CalendarEvent>> GetEventsAsync(string calendarName, DateTime start, DateTime end)
    {
        logger.LogInformation("Fetching calendar events from {Start} to {End} for calendar {CalendarName}", start, end, calendarName);

        var events = await graphService.ExecuteInContextAsync(async (client) =>
        {
            var calendars = await client.Me.Calendars.GetAsync();

            var eventsResponse = await client.Me.CalendarView.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.StartDateTime = start.ToString("o");
                requestConfig.QueryParameters.EndDateTime = end.ToString("o");
                requestConfig.Headers.Add("Prefer", "outlook.timezone=UTC");
                requestConfig.QueryParameters.Top = 100;
            });

            var items = eventsResponse?.Value ?? [];

            return items.Select(e => new CalendarEvent
            {
                Subject = e.Subject,
                Location = e.Location?.DisplayName,
                Start = e.Start?.DateTime,
                End = e.End?.DateTime,
                IsAllDay = e.IsAllDay ?? false,
                BodyPreview = e.BodyPreview
            }).ToList();
        });

        this.logger.LogInformation("Fetched {EventCount} events", events.Count());

        return events;
    }
}   