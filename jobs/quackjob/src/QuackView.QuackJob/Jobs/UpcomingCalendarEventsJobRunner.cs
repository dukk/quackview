using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using Microsoft.IdentityModel.Tokens;
using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Jobs;

[JobRunner("upcoming-calendar-events", "Gets upcoming calendar events")]
internal class UpcomingCalendarEventsJobRunner(
    ILogger<UpcomingCalendarEventsJobRunner> logger,
    IOutlookCalendarEventService outlookCalendarEventService,
    IDataFileService dataFileService,
    IConsoleService console,
    ISecretStore secretStore,
    IDiskIOService file)
    : JobRunner<JobFile<UpcomingCalendarEventsJobConfig>>(file)
{
    protected readonly ILogger<UpcomingCalendarEventsJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IOutlookCalendarEventService OutlookCalendarEventService = outlookCalendarEventService ?? throw new ArgumentNullException(nameof(outlookCalendarEventService));
    protected readonly IDataFileService DataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));
    protected readonly ISecretStore SecretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));

    public override async Task ExecuteJobFileAsync(JobFile<UpcomingCalendarEventsJobConfig> jobFile)
    {
        var config = jobFile.Config ?? throw new ArgumentException("Invalid job configuration.", nameof(jobFile));

        if (config.Accounts.IsNullOrEmpty())
            throw new ArgumentException("Invalid job configuration. No accounts specified.", nameof(config));

        this.Console.WriteLine("Executing Upcoming Calendar Events job.");

        var allEvents = new List<CalendarEvent>();

        foreach (var account in config.Accounts)
        {
            var accountEmail = await this.SecretStore.ExpandSecretsAsync(account.Account);

            allEvents.AddRange(await this.OutlookCalendarEventService.GetEventsAsync(
                accountEmail,
                account.Calendars,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(config.DaysInFuture)));
        }

        this.Console.WriteLine($"Writing output file: {config.OutputDataFilePath}");
        await this.DataFileService.WriteJsonFileAsync(config.OutputDataFilePath, allEvents.OrderBy(e => e.Start));

        return;
    }

    public override async Task CreateNewJobFileAsync(string filePath)
    {
        var content = JsonSerializer.Serialize(new JobFile<UpcomingCalendarEventsJobConfig>()
        {
            Metadata = new()
            {
                Name = "Update calendar events",
                Description = "Updates calendar events",
                Runner = "upcoming-calendar-events",
                Schedule = "* * * * *"
            },
            Config = new()
            {
                DaysInFuture = 14,
                Accounts =
                [
                    new()
                    {
                        Account = "your-account@outlook.com",
                        Calendars = ["Calendar"]
                    }
                ],
                OutputDataFilePath = "calendar/upcoming-events.json"
            }
        }, options: Program.DefaultJsonSerializerOptions);

        await this.Disk.WriteAllTextAsync(filePath, content);
    }
}

internal class UpcomingCalendarEventsJobConfig : FileOutputJobConfig
{
    public UpcomingCalendarEventsJobConfig() : base()
    {
        this.OutputDataFilePath = "calendar/upcoming-events.json";
    }

    public IList<CalendarAccounts> Accounts { get; set; } = [];
    public int DaysInFuture { get; set; } = 14;
}

public class CalendarAccounts()
{
    public string[] Calendars { get; set; } = [];
    public string Account { get; set; } = string.Empty;
}