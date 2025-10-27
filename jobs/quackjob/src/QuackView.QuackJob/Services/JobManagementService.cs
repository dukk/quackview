using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IJobManagementService
{
    Task<IEnumerable<string>> GetAvailableJobsAsync();

    Task<string> GetJobFilePath(string jobName);

    Task SaveJobFile(string jobName, IJobFile jobFile);
    Task DeleteJobAsync(string jobName);
}

internal class JobManagementService(
    ILogger<JobManagementService> logger,
    IDiskIOService disk,
    ISpecialPaths SpecialPaths) : IJobManagementService
{
    protected readonly ILogger<JobManagementService> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDiskIOService Disk = disk ?? throw new ArgumentNullException(nameof(disk));
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));

    public async Task<IEnumerable<string>> GetAvailableJobsAsync()
    {
        var jobFiles = await this.Disk.EnumerateFilesAsync(await this.SpecialPaths.GetJobsDirectoryPathAsync(), "*.json");

        return jobFiles.Select(f => Path.GetFileNameWithoutExtension(f));
    }

    public async Task<string> GetJobFilePath(string jobName)
    {
        var jobFilePath = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), jobName + ".json");

        if (!await this.Disk.FileExistsAsync(jobFilePath))
            throw new ArgumentException($"Unable to find job named '{jobName}'", nameof(jobName));

        return jobFilePath;
    }

    public async Task SaveJobFile(string jobName, IJobFile jobFile)
    {
        ArgumentNullException.ThrowIfNull(jobName);
        ArgumentNullException.ThrowIfNull(jobFile);

        var jobFilePath = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), jobName + ".json");
        var json = jobFile.ToJson();

        await this.Disk.WriteAllTextAsync(jobFilePath, json);
    }

    public async Task DeleteJobAsync(string jobName)
    {
        ArgumentNullException.ThrowIfNull(jobName);

        var jobFilePath = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), jobName + ".json");

        await this.Disk.DeleteFileAsync(jobFilePath);
    }
}