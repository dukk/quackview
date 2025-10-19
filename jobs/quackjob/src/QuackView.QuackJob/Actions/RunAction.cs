using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class RunAction(
    ILogger<RunAction> logger,
    ICommandLineParser commandLineParser,
    IServiceProvider serviceProvider,
    IFileService file,
    IConsoleService console,
    ISpecialPaths SpecialPaths) 
    : Action(logger, console)
{
    protected readonly new ILogger<RunAction> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly ICommandLineParser CommandLineParser = commandLineParser ?? throw new ArgumentNullException(nameof(commandLineParser));
    protected readonly IServiceProvider ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    protected readonly IFileService File = file ?? throw new ArgumentNullException(nameof(file));
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));

    public override async Task ExecuteAsync(string[] args)
    {
        var runnerName = string.Empty;
        JsonElement? jsonConfig = null;
        var parsedArgs = this.CommandLineParser.ParseArgs(args);

        if (parsedArgs.TryGetValue("job", out var jobPath))
        {
            this.Console.WriteLine($"Using job file: {jobPath}");

            if (String.IsNullOrWhiteSpace(jobPath))
                throw new Exception($"Invalid job file '{jobPath}'.");

            // If no directory is specified and the file doesn't exist look for it in the job directory too
            if (!await this.File.ExistsAsync(jobPath))
            {
                var jobDirectory = Path.GetDirectoryName(jobPath);

                if (string.IsNullOrWhiteSpace(jobDirectory))
                {
                    jobDirectory = await this.SpecialPaths.GetJobsDirectoryPathAsync();
                    jobPath = Path.Combine(jobDirectory, jobPath);
                }
            }

            var jobFileContent = await this.File.ReadAllTextAsync(jobPath);
            var jobFile = JsonDocument.Parse(jobFileContent);
            var jobMetadata = jobFile.RootElement.GetProperty("metadata");

            runnerName = jobMetadata.GetProperty("runner").GetString();
            jsonConfig = jobFile.RootElement.GetProperty("config");
        }
        else
        {
            if (!parsedArgs.TryGetValue("runner", out runnerName) || string.IsNullOrEmpty(runnerName))
                throw new Exception("No job runner specified. Use --runner <runner-name> to specify a job runner to run.");

            string? configPath = null!;

            if (parsedArgs.TryGetValue("config", out configPath))
            {
                this.Console.WriteLine($"Using config file: {configPath}");

                if (String.IsNullOrWhiteSpace(configPath))
                    throw new Exception($"Invalid config file path '{configPath}'.");

                jsonConfig = JsonDocument.Parse(await this.File.ReadAllTextAsync(configPath)).RootElement;
            }
        }

        if (runnerName.IsNullOrEmpty())
            throw new Exception("Error: Unspecified job runner.");

        this.Console.WriteLine($"Using job runner: {runnerName}");

        var jobRunner = this.ServiceProvider.GetServices<IJobRunner>()
            .FirstOrDefault(j => j.Name.Equals(runnerName, StringComparison.OrdinalIgnoreCase));

        if (jobRunner == null)
            throw new Exception($"Job runner '{runnerName}' not found.");
        
        await jobRunner.ExecuteAsync(jsonConfig);
    }
}