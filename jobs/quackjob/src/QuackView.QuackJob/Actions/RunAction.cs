using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

// [Action]
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
        if (args.Length < 1)
            throw new Exception("Error: No job file specified.");

        this.Logger.LogDebug("Executing RunAction with args: {args}", string.Join(' ', args));

        var runnerName = string.Empty;
        var jobPath = args[0];

        if (String.IsNullOrWhiteSpace(jobPath))
            throw new Exception($"Invalid job file '{jobPath}'.");

        if (!await this.File.ExistsAsync(jobPath))
        {
            if (!jobPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                jobPath += ".json";

            if (!await this.File.ExistsAsync(jobPath))
            {
                var jobDirectory = Path.GetDirectoryName(jobPath);

                if (string.IsNullOrWhiteSpace(jobDirectory))
                {
                    jobDirectory = await this.SpecialPaths.GetJobsDirectoryPathAsync();
                    jobPath = Path.Combine(jobDirectory, jobPath);
                }
            }
        }

        this.Console.WriteLine($"Using job file: {jobPath}");

        if (!await this.File.ExistsAsync(jobPath))
            throw new FileNotFoundException($"Job file '{jobPath}' does not exist.");

        var jobFileContent = await this.File.ReadAllTextAsync(jobPath);

        this.Logger.LogDebug("Job file content: {jobFileContent}", jobFileContent);

        var jobFile = JsonDocument.Parse(jobFileContent);
        var jobMetadata = jobFile.RootElement.GetProperty("metadata");

        runnerName = jobMetadata.GetProperty("runner").GetString();

        if (runnerName.IsNullOrEmpty())
            throw new Exception("Error: Unspecified job runner.");

        this.Console.WriteLine($"Using job runner: {runnerName}");

        var jobRunners = this.ServiceProvider.GetServices<IJobRunner>();

        this.Logger.LogDebug("Available job runners: {jobRunners}", string.Join(", ", jobRunners.Select(r => r.Name)));

        var jobRunner = jobRunners.FirstOrDefault(j => j.Name.Equals(runnerName, StringComparison.OrdinalIgnoreCase))
            ?? throw new Exception($"Job runner '{runnerName}' not found.");

        await jobRunner.ExecuteJobFileAsync(jobPath);
    }
}