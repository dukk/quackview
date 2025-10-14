using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob;

internal interface IProgram
{
    IHost Host { get; }
    string GetAppDataDirectory();
}

internal class Program : IProgram
{
    public const string SOLUTION_NAME = "TDDashboard";
    public const string APP_NAME = "TDDQuackJob";

    private readonly ILogger<Program> logger;
    private readonly IHost host;

    internal Program(string[] args)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);

        // Adding services:
        hostBuilder.Services.AddSingleton<IProgram>(this);
        hostBuilder.Services.AddSingleton<IJobRunnerService, JobRunnerService>();
        hostBuilder.Services.AddSingleton<IGraphService, GraphService>();
        hostBuilder.Services.AddSingleton<IOutlookCalendarEventService, OutlookCalendarEventService>();
        hostBuilder.Services.AddSingleton<IDataFileService, DataFileService>();

        // Adding jobs (all should be of type IJob):
        hostBuilder.Services.AddSingleton<IJob, UpcomingCalendarEventsJob>();

        this.host = hostBuilder.Build();
        this.logger = this.host.Services.GetRequiredService<ILogger<Program>>();
    }

    IHost IProgram.Host { get { return this.host; } }

    internal async Task<int> Run(string[] args)
    {
#if DEBUG
        this.logger.LogCritical("Running in DEBUG mode - using hardcoded args");

        args = ["run", "upcoming-calendar-events:\"D:\\Development\\dashboard\\jobs\\graph-jobs\\debug\\jobs\\upcoming-calendar-events.json\""];
#endif

        this.logger.LogInformation("Current working directory: {workingDirectory}", Environment.CurrentDirectory);

        if (args.IsNullOrEmpty())
            throw new ArgumentException(nameof(args));

        var action = args[0].ToLower();

        try
        {
            switch (action)
            {
                case "run":
                    await this.runJobs(args[1..]);
                    break;
                case "new":
                    await this.newJob(args[1]);
                    break;
                case "jobs":
                    await this.listJobs();
                    break;
                default:
                    return 2;
            }

            return 0;
        }
        catch (Exception exception)
        {
            this.logger.LogCritical(exception, "An unhandled exception occurred");
            return 1;
        }
    }
    private Task newJob(string v)
    {
        throw new NotImplementedException();
    }

    private Task listJobs()
    {
        throw new NotImplementedException();
    }

    private async Task runJobs(string[] jobArgs)
    {
        var jobRunner = this.host.Services.GetRequiredService<IJobRunnerService>();
        await jobRunner.RunJobsAsync(jobArgs);
    }

    public string GetAppDataDirectory()
    {
        var appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TDDashboard", "TDDQuackJob");

        Directory.CreateDirectory(appDataDirectory);

        this.logger.LogDebug("Using app data directory {appDataDirectory}", appDataDirectory);

        return appDataDirectory;
    }

    public static async Task<int> Main(string[] args)
    {
        return await new Program(args).Run(args);
    }
}
