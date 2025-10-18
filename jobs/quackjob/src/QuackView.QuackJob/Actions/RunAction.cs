using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class RunAction(ILogger<RunAction> logger, ICommandLineParser commandLineParser,
    IServiceProvider serviceProvider, IConsoleService console) : Action(console)
{
    private readonly ILogger<RunAction> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICommandLineParser commandLineParser = commandLineParser ?? throw new ArgumentNullException(nameof(commandLineParser));
    private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public override async Task ExecuteAsync(string[] args)
    {
        var parsedArgs = this.commandLineParser.ParseArgs(args);

        if (!parsedArgs.TryGetValue("job", out var jobFile) || string.IsNullOrEmpty(jobFile))
        {
            
        }

        if (!parsedArgs.TryGetValue("runner", out var runnerName) || string.IsNullOrEmpty(runnerName))
        {
            console.WriteError("Error: No job runner specified. Use --runner <runner-name> to specify a job runner to run.");
            return;
        }

        console.WriteLine($"Using job runner: {runnerName}");

        var job = this.serviceProvider.GetServices<IJobRunner>()
            .FirstOrDefault(j => j.Name.Equals(runnerName, StringComparison.OrdinalIgnoreCase));

        if (job == null)
        {
            console.WriteError($"Error: Job runner '{runnerName}' not found.");
            return;
        }

        string? configFile = null!;
        if (parsedArgs.TryGetValue("config", out configFile) && !string.IsNullOrEmpty(configFile))
            console.WriteLine($"Using config file: {configFile}");
        
        await job.ExecuteAsync(configFile, parsedArgs);
    }
}