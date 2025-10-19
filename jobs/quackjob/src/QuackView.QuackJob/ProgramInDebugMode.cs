
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
    protected readonly string QuackviewOverridePath = ProgramInDebugMode.GetQuackviewOverridePath();
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));

    public override Task<int> Run(string[] args)
    {
        System.Console.BackgroundColor = ConsoleColor.Red;
        System.Console.ForegroundColor = ConsoleColor.Black;

        System.Console.WriteLine("RUNNING IN DEBUG MODE USER INPUT COULD BE OVERWRITTEN!!!");

        // TODO: Should really move all of this to unit tests (or integration tests?)...

        // Run Jobs:
        //args = ["run", "--job=\"build-image-file-list.json\""];
        //args = ["run", "--job=\"open-ai-prompt.json\""];
        //args = ["run", "--job=\"upcoming-calendar-events.json\""];
        //args = ["run", "--job=\"dad-jokes.json\""];
        //args = ["run", "--job=\"clear-expired-alerts.json\""];

        // Rebuild Schedule:
        args = ["rebuild-schedule"];

        // Help:
        //args = ["help"];
        //args = ["help", "run"];
        //args = ["help", "list"];
        //args = ["help", "new"];

        // List Job Runners (change to 'list-runners'?)
        args = ["list"];

        // New Jobs  (change to 'new-job'?)
        args = ["new"];

        System.Console.WriteLine($"ARGS: {string.Join(' ', args)}");
        System.Console.ResetColor();

        this.EnsureDebugFiles();

        return base.Run(args);
    }

    public void EnsureDebugFiles()
    {
        if (!Directory.Exists(this.QuackviewOverridePath))
            Directory.CreateDirectory(this.QuackviewOverridePath);

        var configPath = Path.Combine(this.QuackviewOverridePath, "config");
        var dataPath = Path.Combine(this.QuackviewOverridePath, "data");
        var jobsPath = Path.Combine(this.QuackviewOverridePath, "jobs");
        var secretsPath = Path.Combine(this.QuackviewOverridePath, "secrets");

        if (!Directory.Exists(configPath))
            Directory.CreateDirectory(configPath);

        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        if (!Directory.Exists(jobsPath))
            Directory.CreateDirectory(jobsPath);

        if (!Directory.Exists(secretsPath))
            Directory.CreateDirectory(secretsPath);

        var buildImageFileListJobPath = Path.Combine(jobsPath, "build-image-file-list.json");
        if (!File.Exists(buildImageFileListJobPath))
        {
            var content = JsonSerializer.Serialize(new JobFile<BuildImageFileListJobConfig>()
            {
                Metadata = new()
                {
                    Name = "Build image file list",
                    Description = "Build a list of image files that can be used to cycle through by the display",
                    Runner = "build-image-file-list",
                    Schedule = "* * * * *"
                },
                Config = new()
                {
                    DirectoryPath = "photos",
                    IncludeSubdirectories = false,
                    SearchPattern = "*.jpg",
                    OutputDataFile = "photos/list.json"
                }
            }, options: Program.DefaultJsonSerializerOptions);
            File.WriteAllText(buildImageFileListJobPath, content);
        }

        var openAiPromptJobPath = Path.Combine(jobsPath, "open-ai-prompt.json");
        if (!File.Exists(openAiPromptJobPath))
        {
            var content = JsonSerializer.Serialize(new JobFile<OpenAiPromptJobConfig>()
            {
                Metadata = new()
                {
                    Name = "Open AI Prompt",
                    Description = "Generate json files based on a prompt for Open AI prompt",
                    Runner = "open-ai-prompt",
                    Schedule = "* * * * *"
                },
                Config = new()
                {
                    Prompt = "",
                    ApiKey = "$^{open-ai-api-key}"
                }
            }, options: Program.DefaultJsonSerializerOptions);
            File.WriteAllText(openAiPromptJobPath, content);
        }

        var upcomingCalendarEventsJobPath = Path.Combine(jobsPath, "upcoming-calendar-events.json");
        if (!File.Exists(upcomingCalendarEventsJobPath))
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
                    OutputFileName = "calendar/upcoming.json"
                }
            }, options: Program.DefaultJsonSerializerOptions);
            File.WriteAllText(upcomingCalendarEventsJobPath, content);
        }

        var clearExpiredAlertsJobPath = Path.Combine(jobsPath, "clear-expired-alerts.json");
        if (!File.Exists(clearExpiredAlertsJobPath))
        {
            var content = JsonSerializer.Serialize(new JobFile<object>()
            {
                Metadata = new()
                {
                    Name = "Clear expired alerts",
                    Description = "Clears expired alerts",
                    Runner = "clear-expired-alerts",
                    Schedule = "*/10 * * * *"
                }
            }, options: Program.DefaultJsonSerializerOptions);
            File.WriteAllText(clearExpiredAlertsJobPath, content);
        }
    }

    internal static string GetQuackviewOverridePath([CallerFilePath] string sourceFilePath = "")
    {
        //System.Console.WriteLine($"ProgramInDebugMode: sourceFilePath = '{sourceFilePath}'");

        var path = Path.GetDirectoryName(sourceFilePath) ?? throw new Exception("Can't get path...");
        // Traverse the path up to get to the project root (ugly, I tried a few ways but it works...)
        path = Path.GetDirectoryName(path) ?? throw new Exception("Cannot get parent directory (level 1).");
        path = Path.GetDirectoryName(path) ?? throw new Exception("Cannot get parent directory (level 2).");
        path = Path.Combine(path, "debug");

        //System.Console.WriteLine($"ProgramInDebugMode: path = '{path}'");

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