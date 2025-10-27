
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob;

[ExcludeFromCodeCoverage]
internal class ProgramInDebugMode(
    ILogger<ProgramInDebugMode> logger,
    IServiceProvider serviceProvider,
    IConsoleService console,
    ISpecialPaths SpecialPaths)
    : Program(logger, serviceProvider, console)
{
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));

    public async override Task<int> Run(string[] args)
    {
        System.Console.BackgroundColor = ConsoleColor.Red;
        System.Console.ForegroundColor = ConsoleColor.Black;

        System.Console.WriteLine("RUNNING IN DEBUG MODE USER INPUT COULD BE OVERWRITTEN!!!");

        // TODO: Should really move all of this to unit tests (or integration tests?)...

        // Run Jobs:
        // args = ["run", "build-image-file-list.json"];
        // args = ["run", "open-ai-prompt.json"];
        // args = ["run", "upcoming-calendar-events.json"];
        // args = ["run", "dad-jokes.json"];
        // args = ["run", "ai-dad-jokes.json"];
        // args = ["run", "clear-expired-alerts.json"];
        // args = ["run", "ai-current-weather.json"]; // doesn't work
        // args = ["run", "ai-forecast-weather.json"];
        // args = ["run", "ai-local-news.json"];
        // args = ["run", "ai-us-news.json"];
        // args = ["run", "ai-world-news.json"];
        // args = ["run", "current-weather-owm.json"];
        args = ["run", "rss-world-news.json"];
        // args = ["run", "rss-us-news.json"];
        // args = ["run", "rss-local-news.json"];

        // Rebuild Schedule:
        // args = ["rebuild-schedule"];

        // Help:
        // args = [];
        // args = ["help"];
        // args = ["help", "run"];
        // args = ["help", "list"];
        // args = ["help", "new"];

        // List Job Runners (change to 'list-runners'?)
        // args = ["list"];

        // New Jobs  (change to 'new-job'?)
        // args = ["new"];

        System.Console.WriteLine($"ARGS: {string.Join(' ', args)}");
        System.Console.ResetColor();

        await this.EnsureDebugFiles();

        return await base.Run(args);
    }

    public async Task EnsureDebugFiles()
    {
        var quackviewOverridePath = await this.SpecialPaths.GetQuackViewDirectoryAsync();
        var configPath = await this.SpecialPaths.GetConfigDirectoryPathAsync();
        var dataPath = await this.SpecialPaths.GetDataDirectoryPathAsync();
        var photosPath = Path.Combine(dataPath, "photos");
        var calendarPath = Path.Combine(dataPath, "calendar");
        var jobsPath = await this.SpecialPaths.GetJobsDirectoryPathAsync();
        var secretsPath = await this.SpecialPaths.GetSecretsDirectoryPathAsync();

        foreach (var path in new string[] { quackviewOverridePath, configPath, dataPath, photosPath, calendarPath, jobsPath, secretsPath })
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        var buildImageFileListJobPath = Path.Combine(jobsPath, "build-image-file-list.json");
        if (!File.Exists(buildImageFileListJobPath))
        {
            var jobRunner = this.ServiceProvider.GetService<BuildImageFileListJobRunner>();
            if (jobRunner != null)
                await jobRunner.CreateNewJobFileAsync(buildImageFileListJobPath);
        }

        var openAiPromptJobPath = Path.Combine(jobsPath, "open-ai-prompt.json");
        if (!File.Exists(openAiPromptJobPath))
        {
            var jobRunner = this.ServiceProvider.GetService<OpenAiPromptJobRunner>();
            if (jobRunner != null)
                await jobRunner.CreateNewJobFileAsync(openAiPromptJobPath);
        }

        var upcomingCalendarEventsJobPath = Path.Combine(jobsPath, "upcoming-calendar-events.json");
        if (!File.Exists(upcomingCalendarEventsJobPath))
        {
            var jobRunner = this.ServiceProvider.GetService<UpcomingCalendarEventsJobRunner>();
            if (jobRunner != null)
                await jobRunner.CreateNewJobFileAsync(upcomingCalendarEventsJobPath);
        }

        var clearExpiredAlertsJobPath = Path.Combine(jobsPath, "clear-expired-alerts.json");
        if (!File.Exists(clearExpiredAlertsJobPath))
        {
            var jobRunner = this.ServiceProvider.GetService<ClearExpiredAlertsJobRunner>();
            if (jobRunner != null)
                await jobRunner.CreateNewJobFileAsync(clearExpiredAlertsJobPath);
        }
    }

    internal static string GetQuackviewOverridePath([CallerFilePath] string sourceFilePath = "")
    {
        System.Console.WriteLine($"ProgramInDebugMode: sourceFilePath = '{sourceFilePath}'");

        var path = Path.GetDirectoryName(sourceFilePath) ?? throw new Exception("Can't get path...");
        // Traverse the path up to get to the project root (ugly, I tried a few ways but it works...)
        path = Path.GetDirectoryName(path) ?? throw new Exception("Cannot get parent directory (level 1).");
        path = Path.GetDirectoryName(path) ?? throw new Exception("Cannot get parent directory (level 2).");
        path = Path.Combine(path, "debug");

        System.Console.WriteLine($"ProgramInDebugMode: path = '{path}'");

        return path;
    }
}

[ExcludeFromCodeCoverage]
internal class DebugSpecialPaths : SpecialPaths
{
    protected readonly string QuackviewOverridePath;

    public DebugSpecialPaths(ILogger<SpecialPaths> logger, IDirectoryService directory)
        : base(logger, directory)
    {
        /* Cannot use the primary constructor here because the code generation will 
            show this call coming from a library and not this file. */
        this.QuackviewOverridePath = ProgramInDebugMode.GetQuackviewOverridePath();

        Environment.SetEnvironmentVariable("QUACKVIEW_DIR", this.QuackviewOverridePath);
    }

    public override Task<string> GetQuackViewDirectoryAsync()
    {
        return Task<string>.FromResult(this.QuackviewOverridePath);
    }
}