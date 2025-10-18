using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using TypoDukk.QuackView.QuackJob.Jobs;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IJobManagementService
{
    Task<IEnumerable<string>> GetAvailableJobFilesAsync();
    Task<JobFile> LoadJobFileAsync(string path);
    Task SaveJobFileAsync(string path, JobFile jobFile, bool overwrite = false);
    Task DeleteJobFileAsync(string path);
    string GetJobsDirectory();
}

internal class JobManagementService(ILogger<JobManagementService> logger,
    IDirectoryService directory, IFileService file) : IJobManagementService
{
    private readonly ILogger<JobManagementService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));
    private readonly IFileService fileService = file ?? throw new ArgumentNullException(nameof(file));

    public async Task<IEnumerable<string>> GetAvailableJobFilesAsync()
    {
        return await this.directory.EnumerateFilesAsync(this.GetJobsDirectory(), "*.json");
    }

    public async Task<JobFile> LoadJobFileAsync(string path)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = Path.Combine(this.GetJobsDirectory(), path);

        if (!await this.fileService.ExistsAsync(path))
            throw new FileNotFoundException("Job file not found.", path);

        var content = await this.fileService.ReadAllTextAsync(path);
        var jobFile = JsonSerializer.Deserialize<JobFile>(content)
            ?? throw new ArgumentException("Failed to read job file. Do you have a JSON syntax error?", nameof(path));

        return jobFile;
    }

    public async Task SaveJobFileAsync(string path, JobFile jobFile, bool overwrite = false)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = Path.Combine(this.GetJobsDirectory(), path);

        if (await this.fileService.ExistsAsync(path) && !overwrite)
            throw new FileNotFoundException("Job file already exists and overwrite was not specified.", path);

        var content = JsonSerializer.Serialize(jobFile);

        await this.fileService.WriteAllTextAsync(path, content);
    }

     public async Task DeleteJobFileAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = Path.Combine(this.GetJobsDirectory(), path);

        await fileService.DeleteFileAsync(path);
    }

    public string GetJobsDirectory()
    {
        var quackviewDir = Environment.GetEnvironmentVariable("QUACKVIEW_DIR")
            ?? throw new InvalidOperationException("Missing QUACKVIEW_DIR env var.");

        return Path.Combine(quackviewDir, "jobs");
    }
} 