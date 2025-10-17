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

    // TODO: Validate that we don't have actions that would match the same name etc...

        return hostBuilder.Build();
    }

    internal static void ComposeServices(IServiceCollection services)
    {
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDirectoryService, DirectoryService>();
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<IJobRunnerService, JobRunnerService>();
        services.AddSingleton<ICronScheduler, CronScheduler>();
        services.AddSingleton<ISecretStore, SecretStore>();
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IOutlookCalendarEventService, OutlookCalendarEventService>();
        services.AddSingleton<IDataFileService, DataFileService>();
    }

    internal static void ComposeActions(IServiceCollection services)
    {
        services.AddSingleton<IAction, HelpAction>();
        services.AddSingleton<IAction, RunAction>();
    }

    internal static void ComposeJobs(IServiceCollection services)
    {
        services.AddSingleton<IJob, UpcomingCalendarEventsJob>();
        services.AddSingleton<IJob, OpenAiPromptJob>();
    }
}