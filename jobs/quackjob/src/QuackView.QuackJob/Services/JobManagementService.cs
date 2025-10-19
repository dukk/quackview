using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using TypoDukk.QuackView.QuackJob.Jobs;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IJobManagementService
{
    Task<IEnumerable<string>> GetAvailableJobFilesAsync();
    Task<JobFile<TConfig>> LoadJobFileAsync<TConfig>(string path);
    Task SaveJobFileAsync<TConfig>(string path, JobFile<TConfig> jobFile, bool overwrite = false);
    Task DeleteJobFileAsync(string path);
}

internal class JobManagementService(ILogger<JobManagementService> logger,
    IDirectoryService directory, IFileService file, ISpecialPaths SpecialPaths) : IJobManagementService
{
    protected readonly ILogger<JobManagementService> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDirectoryService Directory = directory ?? throw new ArgumentNullException(nameof(directory));
    protected readonly IFileService FileService = file ?? throw new ArgumentNullException(nameof(file));
    protected readonly ISpecialPaths SpecialPaths = SpecialPaths ?? throw new ArgumentNullException(nameof(SpecialPaths));

    public async Task<IEnumerable<string>> GetAvailableJobFilesAsync()
    {
        return await this.Directory.EnumerateFilesAsync(await this.SpecialPaths.GetJobsDirectoryPathAsync(), "*.json");
    }

    public async Task<JobFile<TConfig>> LoadJobFileAsync<TConfig>(string path)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), path);

        if (!await this.FileService.ExistsAsync(path))
            throw new FileNotFoundException("Job file not found.", path);

        var content = await this.FileService.ReadAllTextAsync(path);
        var jobFile = JsonSerializer.Deserialize<JobFile<TConfig>>(content)
            ?? throw new ArgumentException("Failed to read job file. Do you have a JSON syntax error?", nameof(path));

        return jobFile;
    }

    public async Task SaveJobFileAsync<TConfig>(string path, JobFile<TConfig> jobFile, bool overwrite = false)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), path);

        if (await this.FileService.ExistsAsync(path) && !overwrite)
            throw new FileNotFoundException("Job file already exists and overwrite was not specified.", path);

        var content = JsonSerializer.Serialize(jobFile);

        await this.FileService.WriteAllTextAsync(path, content);
    }

     public async Task DeleteJobFileAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = Path.Combine(await this.SpecialPaths.GetJobsDirectoryPathAsync(), path);

        await this.FileService.DeleteFileAsync(path);
    }
} 