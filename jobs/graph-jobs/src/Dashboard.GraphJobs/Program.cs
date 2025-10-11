using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TypoDukk.Dashboard.GraphJobs.Jobs;
using TypoDukk.Dashboard.GraphJobs.Services;

namespace TypoDukk.Dashboard.GraphJobs;

internal interface IProgram
{
    IHost Host { get; }
    string GetAppDataDirectory();
}

internal class Program : IProgram
{
    public const string SOLUTION_NAME = "TDDashboard";
    public const string APP_NAME = "TDDGraphJobs";

    private readonly ILogger<Program> logger;
    private readonly IHost host;

    internal Program(string[] args)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);

        // Adding services:
        hostBuilder.Services.AddSingleton<IProgram>(this);
        hostBuilder.Services.AddSingleton<IJobRunnerService, JobRunnerService>();
        hostBuilder.Services.AddSingleton<IGraphService, GraphService>();
        hostBuilder.Services.AddSingleton<ICalendarEventService, CalendarEventService>();
        hostBuilder.Services.AddSingleton<IDataFileService, DataFileService>();

        // Adding jobs (all should be of type IJob):
        hostBuilder.Services.AddSingleton<IJob, UpcomingCalendarEventsJob>();
        hostBuilder.Services.AddSingleton<IJob, RandomPhotosJob>();

        this.host = hostBuilder.Build();
        this.logger = this.host.Services.GetRequiredService<ILogger<Program>>();
    }

    IHost IProgram.Host { get { return this.host; } }

    internal async Task<int> Run(string[] args)
    {
#if DEBUG
        this.logger.LogCritical("Running in DEBUG mode - using hardcoded args");

        args = ["run", "upcoming-calendar-events:./debug/jobs/upcoming-calendar-events.json"];
#endif
        try
        {
            var jobRunner = this.host.Services.GetRequiredService<IJobRunnerService>();
            await jobRunner.RunJobsAsync(args);
            return 0;
        }
        catch
        {
            return 1;
        }
    }

    public string GetAppDataDirectory()
    {
        var appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TDDashboard", "TDDGraphJobs");

        Directory.CreateDirectory(appDataDirectory);

        this.logger.LogDebug("Using app data directory {appDataDirectory}", appDataDirectory);

        return appDataDirectory;
    }

    public static async Task<int> Main(string[] args)
    {
        return await new Program(args).Run(args);
    }
}
