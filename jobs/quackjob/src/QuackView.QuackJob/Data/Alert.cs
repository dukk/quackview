namespace TypoDukk.QuackView.QuackJob.Data;

internal class Alert
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; } = AlertSeverity.None;
    public DateTime Effective { get; set; } = DateTime.UtcNow;
    public DateTime Expires { get; set; } = DateTime.UtcNow.AddMinutes(30);
}

internal enum AlertSeverity
{
    None,
    Minor,
    Moderate,
    Severe
}