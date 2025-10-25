using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using TypoDukk.QuackView.QuackJob.Jobs;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IJobManagementService
{
    Task<IEnumerable<string>> GetAvailableJobsAsync();

    Task<string> GetJobFilePath(string jobName);

    Task SaveJobFile(string jobName, IJobFile jobFile);
    Task DeleteJobAsync(string jobName);
}

internal class JobManagementService(ILogger<JobManagementService> logger,
    IDirectoryService directory, IFileService file, ISpecialPaths SpecialPaths) : IJobManagementService
{
    protected readonly ILogger<JobManagementService> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDirectoryService Directory = directory ?? throw new ArgumentNullException(nameof(directory));
    protected readonly IFileService File = file ?? throw new ArgumentNullException(nameof(file));
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));

    public async Task<IEnumerable<string>> GetAvailableJobsAsync()
    {
        var jobFiles = await this.Directory.EnumerateFilesAsync(await this.SpecialPaths.GetJobsDirectoryPathAsync(), "*.json");

        return jobFiles.Select(f => Path.GetFileNameWithoutExtension(f));
    }

    public async Task<string> GetJobFilePath(string jobName)
    {
        var jobFilePath = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), jobName + ".json");

        if (!await this.File.ExistsAsync(jobFilePath))
            throw new ArgumentException($"Unable to find job named '{jobName}'", nameof(jobName));

        return jobFilePath;
    }

    public async Task SaveJobFile(string jobName, IJobFile jobFile)
    {
        ArgumentNullException.ThrowIfNull(jobName);
        ArgumentNullException.ThrowIfNull(jobFile);

        var jobFilePath = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), jobName + ".json");
        var json = jobFile.ToJson();

        await this.File.WriteAllTextAsync(jobFilePath, json);
    }

    public async Task DeleteJobAsync(string jobName)
    {
        ArgumentNullException.ThrowIfNull(jobName);

        var jobFilePath = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), jobName + ".json");

        await this.File.DeleteFileAsync(jobFilePath);
    }
}