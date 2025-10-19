using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface ISpecialPaths
{
    Task<string> GetConfigDirectoryPathAsync();
    Task<string> GetDataDirectoryPathAsync();
    Task<string> GetJobsDirectoryPathAsync();
    Task<string> GetQuackViewDirectoryAsync();
    Task<string> GetSecretsDirectoryPathAsync();
    Task<string> GetCrontabFilePathAsync();
    Task<string> GetQuackJobExecutablePathAsync();
}

internal class SpecialPaths(ILogger<SpecialPaths> logger, IDirectoryService directory) : ISpecialPaths
{
    public const string SecretsDirectoryName = "secrets";
    public const string DataDirectoryName = "data";
    public const string ConfigDirectoryName = "config";
    public const string JobsDirectoryName = "jobs";

    protected readonly ILogger<SpecialPaths> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDirectoryService Directory = directory ?? throw new ArgumentNullException(nameof(directory));

    public virtual async Task<string> GetQuackViewDirectoryAsync()
    {
        return await this.EnsureExistsAndReturn(Environment.GetEnvironmentVariable("QUACKVIEW_DIR") ?? string.Empty);
    }

    public virtual async Task<string> GetSecretsDirectoryPathAsync()
    {
        return await this.EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialPaths.SecretsDirectoryName));
    }

    public virtual async Task<string> GetDataDirectoryPathAsync()
    {
        return await this.EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialPaths.DataDirectoryName));
    }

    public virtual async Task<string> GetConfigDirectoryPathAsync()
    {
        return await this.EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialPaths.ConfigDirectoryName));
    }

    public virtual async Task<string> GetJobsDirectoryPathAsync()
    {
        return await this.EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialPaths.JobsDirectoryName));
    }

    public virtual async Task<string> GetCrontabFilePathAsync()
    {
        return Path.Combine(await this.GetConfigDirectoryPathAsync(), "crontab");
    }

    public Task<string> GetQuackJobExecutablePathAsync()
    {
        return Task.FromResult(Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the path of the current executable."));
    }
    
    protected virtual async Task<string> EnsureExistsAndReturn(string directory)
    {
        await this.Directory.CreateDirectoryAsync(directory);

        return directory;
    }
}