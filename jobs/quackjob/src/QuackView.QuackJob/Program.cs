using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TypoDukk.QuackView.QuackJob.Actions;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob;

internal class Program(IServiceProvider serviceProvider, ILogger<Program> logger, IConsoleService console)
{
    public const string SOLUTION_NAME = "TDDashboard";
    public const string APP_NAME = "TDDQuackJob";

    private readonly ILogger<Program> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IConsoleService console = console ?? throw new ArgumentNullException(nameof(console));

    internal async Task Run(string[] args)
    {
        this.logger.LogInformation("Current working directory: {workingDirectory}", Environment.CurrentDirectory);

        var requestedAction = ((args.Length > 0 && !string.IsNullOrEmpty(args[0])) ? args[0] : "help").ToLower();

        if (requestedAction == "help" && args.Length > 1)
        {
            var requestedSubHelpAction = args[1].ToString();
            var subHelpAction = getAction(requestedSubHelpAction);

            if (null != subHelpAction)
            {
                subHelpAction.DisplayHelp();
                return;
            }
            else
            {
                console.WriteError("Error: Unknown action.");
                // Let this continue so it shows the main help display
            }
        }
        
        var action = getAction(requestedAction);

        if (action == null)
        {
            console.WriteError("Error: Unknown action.");
            action = getAction("help");
        }

        await action.ExecuteAsync(args[1..]);
    }

    private IAction getAction(string name)
    {
        name = name.ToLower();

        var action = this.serviceProvider.GetServices<IAction>()
                .FirstOrDefault(a => a.MatchesActionName(name));

#pragma warning disable CS8603 // Possible null reference return.
        return action;
#pragma warning restore CS8603 // Possible null reference return.
    }

    public string GetAppDataDirectory()
    {
        var appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TDDashboard", "TDDQuackJob");

        Directory.CreateDirectory(appDataDirectory);

        this.logger.LogDebug("Using app data directory {appDataDirectory}", appDataDirectory);

        return appDataDirectory;
    }

    [ExcludeFromCodeCoverage]
    public static async Task<int> Main(string[] args)
    {
#if DEBUG
        Environment.SetEnvironmentVariable("QUACKVIEW_DIR",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuackView"));
#endif

        try
        {
            var host = Program.BuildApplication();
            var program = new Program(
                host.Services.GetRequiredService<IServiceProvider>(),
                host.Services.GetRequiredService<ILogger<Program>>(),
                host.Services.GetRequiredService<IConsoleService>()
            );

            await program.Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("CRITICAL UNHANDLED EXCEPTION!!!");
            Console.Error.WriteLine(exception.ToString());

            return 1;
        }

        return 0;
    }
    // These static methods below are separated for easier testing

    internal static IHost BuildApplication()
    {
        var hostBuilder = Host.CreateApplicationBuilder();

        hostBuilder.Configuration.AddJsonFile(Environment.ExpandEnvironmentVariables("%QUACKVIEW_DIR%config/quackjob.json"),
            optional: true, reloadOnChange: true);

        Program.ComposeServices(hostBuilder.Services);
        Program.ComposeActions(hostBuilder.Services);
        Program.ComposeJobs(hostBuilder.Services);

        var host = hostBuilder.Build();

#if DEBUG
        // Extra check to make sure I'm not being dumb...
        Program.VerifyNoDuplicationActions(host);
        Program.VerifyNoDuplicationJobs(host);
#endif
        return host;
    }

    [ExcludeFromCodeCoverage]
    internal static void VerifyNoDuplicationActions(IHost host)
    {
        var actions = host.Services.GetService<IAction>();
        var registeredActions = host.Services.GetServices<IAction>().ToList();
        var duplicateActionGroups = registeredActions
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateActionGroups.Count > 0)
        {
            var details = string.Join("; ", duplicateActionGroups.Select(g =>
                $"{g.Key}: {string.Join(", ", g.Select(a => a.GetType().FullName))}"));
            throw new InvalidOperationException($"Duplicate action names detected: {details}");
        }
    }

    [ExcludeFromCodeCoverage]
    internal static void VerifyNoDuplicationJobs(IHost host)
    {
        var jobs = host.Services.GetService<IJob>();
        var registeredJobs = host.Services.GetServices<IJob>().ToList();
        var duplicateJobGroups = registeredJobs
            .GroupBy(j => j.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateJobGroups.Count > 0)
        {
            var details = string.Join("; ", duplicateJobGroups.Select(g =>
                $"{g.Key}: {string.Join(", ", g.Select(a => a.GetType().FullName))}"));
            throw new InvalidOperationException($"Duplicate job names detected: {details}");
        }
    }

    internal static void ComposeServices(IServiceCollection services)
    {
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDataFileService, DataFileService>();
        services.AddSingleton<IDirectoryService, DirectoryService>();
        services.AddSingleton<IDataDirectoryService, DataDirectoryService>();
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<IJobRunnerService, JobRunnerService>();
        services.AddSingleton<ICronScheduler, CronScheduler>();
        services.AddSingleton<ISecretStore, FileSystemSecretStore>();
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IOutlookCalendarEventService, OutlookCalendarEventService>();
    }

    internal static void ComposeActions(IServiceCollection services)
    {
        services.AddSingleton<IAction, HelpAction>();
        services.AddSingleton<IAction, RunAction>();
        services.AddSingleton<IAction, ListAction>();
        services.AddSingleton<IAction, RebuildScheduleAction>();
    }

    internal static void ComposeJobs(IServiceCollection services)
    {
        services.AddSingleton<IJob, OpenAiPromptJob>();
        services.AddSingleton<IJob, UpcomingCalendarEventsJob>();
        services.AddSingleton<IJob, BuildImageFileListJob>();
    }
}