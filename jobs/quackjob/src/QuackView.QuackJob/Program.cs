using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Actions;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob;

interface IProgram
{
    Task<int> Run(string[] args);
}

internal class Program(
    ILogger<Program> logger,
    IServiceProvider serviceProvider,
    IConsoleService console) 
    : IProgram
{
    public static JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };

    public const string SolutionName = "Quack View";
    public const string SolutionPathName = "quackview";
    public const string ApplicationName = "Quack Jobs";
    public const string ApplicationPathName = "quackjob";

    protected readonly ILogger<Program> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IServiceProvider ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));

    public virtual async Task<int> Run(string[] args)
    {
        this.Logger.LogInformation("Current working directory: {workingDirectory}", Environment.CurrentDirectory);

        var result = 0;
        var requestedAction = ((args.Length > 0 && !string.IsNullOrEmpty(args[0])) ? args[0] : "help").ToLower();

        if (requestedAction == "help" && args.Length > 1)
        {
            var requestedSubHelpAction = args[1].ToString();
            var subHelpAction = this.GetAction(requestedSubHelpAction);

            if (null == subHelpAction)
            {
                console.WriteError("Error: Unknown action.");
                // Let this continue so it shows the main help display
                result = 1;
            }
            else
                subHelpAction.DisplayHelp();
        }

        var action = this.GetAction(requestedAction);

        if (action == null) // null check is here
        {
            this.Console.WriteError("Error: Unknown action.");
            action = this.GetAction("help") 
                ?? throw new InvalidOperationException("Failed to find help action!!!");
            result = 1;
        }

        try
        {
            await action.ExecuteAsync(args[1..]);
        }
        catch (Exception exception)
        {
            console.WriteError($"Error: {exception.Message}");
            result = 1;
        }

        return result;
    }

    protected virtual IAction? GetAction(string name)
    {
        name = name.ToLower();

        var action = this.ServiceProvider.GetServices<IAction>()
                .FirstOrDefault(a => a.MatchesActionName(name));

        return action;
    }

    [ExcludeFromCodeCoverage]
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var host = Program.BuildApplication();
            var program = host.Services.GetRequiredService<IProgram>();

            return await program.Run(args);
        }
        catch (Exception exception)
        {
            System.Console.Error.WriteLine("CRITICAL UNHANDLED EXCEPTION!!!");
            System.Console.Error.WriteLine(exception.ToString());

            return 1;
        }
    }

    internal static IHost BuildApplication()
    {
        var hostBuilder = Host.CreateApplicationBuilder();

        hostBuilder.Configuration.AddJsonFile(
            Path.Combine(Environment.GetEnvironmentVariable("QUACKVIEW_DIR") ?? string.Empty, "config/quackjob.json"),
            optional: true, reloadOnChange: true);

        Program.ComposeServices(hostBuilder.Services);
        Program.ComposeActions(hostBuilder.Services);
        Program.ComposeJobRunners(hostBuilder.Services);
        Program.ComposeDebugInjectionPoint(hostBuilder.Services);

        return hostBuilder.Build();
    }

    internal static void ComposeServices(IServiceCollection services)
    {
        services.AddSingleton<ISpecialPaths, SpecialPaths>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDataFileService, DataFileService>();
        services.AddSingleton<IDirectoryService, DirectoryService>();
        services.AddSingleton<IAlertService, AlertService>();
        services.AddSingleton<IDataDirectoryService, DataDirectoryService>();
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<ICronScheduler, CronScheduler>();
        services.AddSingleton<ISecretStore, FileSystemSecretStore>();
        services.AddSingleton<IMicrosoftGraphService, MicrosoftGraphService>();
        services.AddSingleton<IOutlookCalendarEventService, OutlookCalendarEventService>();
    }

    internal static void ComposeActions(IServiceCollection services)
    {
        services.AddSingleton<IAction, HelpAction>();
        services.AddSingleton<IAction, RunAction>();
        services.AddSingleton<IAction, ListAction>();
        services.AddSingleton<IAction, RebuildScheduleAction>();
    }

    internal static void ComposeJobRunners(IServiceCollection services)
    {
        services.AddSingleton<IJobRunner, OpenAiPromptJob>();
        services.AddSingleton<IJobRunner, UpcomingCalendarEventsJob>();
        services.AddSingleton<IJobRunner, BuildImageFileListJob>();
    }

    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    internal static void ComposeDebugInjectionPoint(IServiceCollection services,
        [CallerFilePath] string sourceFilePath = "")
    {
        System.Console.WriteLine($"ComposeDebugInjectionPoint: sourceFilePath = '{sourceFilePath}'");

        services.RemoveAll<IProgram>();
        services.RemoveAll<ISpecialPaths>();

        services.AddSingleton<IProgram, ProgramInDebugMode>();
        services.AddSingleton<ISpecialPaths, DebugSpecialPaths>();
    }
}