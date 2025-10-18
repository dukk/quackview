using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using TypoDukk.QuackView.QuackJob.Jobs;

internal interface IJobRunnerService
{
    Task RunJobsAsync(string[] jobArgs);
    Task RunJobsAsync(JobRun[] jobs);
}

internal class JobRunnerService(ILogger<JobRunnerService> logger, IServiceProvider serviceProvider) : IJobRunnerService
{
    private readonly ILogger<JobRunnerService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public async Task RunJobsAsync(string[] jobArgs)
    {
        this.logger.LogInformation("Running jobs using arguments {jobArgs}", jobArgs);

        var jobs = this.serviceProvider.GetServices<IJob>();

        this.logger.LogDebug("Found {JobCount} jobs", jobs.Count());

        var jobRuns = jobArgs.Select((jobArg) => JobRun.FromArgString(jobArg, jobs)).ToArray();

        await RunJobsAsync(jobRuns);
    }

    public async Task RunJobsAsync(JobRun[] jobs)
    {
        await Task.WhenAll(jobs.Select(job => job.ExecuteAsync()));
    }
}

internal class JobRun(IJob job, string configPath)
{
    public IJob Job { get; private set; } = job ?? throw new ArgumentNullException(nameof(job));
    public string ConfigPath { get; private set; } = configPath ?? throw new ArgumentNullException(nameof(configPath));

    public async Task ExecuteAsync()
    {
        await this.Job.ExecuteFromConfigFileAsync(this.ConfigPath);
    }

    public static JobRun FromArgString(string arg, IEnumerable<IJob> jobs)
    {
        if (String.IsNullOrWhiteSpace(arg))
            throw new ArgumentNullException(nameof(arg));

        var pair = arg.ToLower().Split(':', 2);
        var job = jobs.FirstOrDefault(j => j.Name.Equals(pair[0]))
            ?? throw new ArgumentException("Unknown job.", nameof(arg));

        return new JobRun(job, pair[1].Trim('\'', '"'));
    }
}