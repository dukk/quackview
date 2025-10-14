namespace TypoDukk.Dashboard.GraphJobs.Data.Shared;

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