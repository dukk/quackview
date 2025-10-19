using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface ISpecialDirectories
{
    Task<string> GetConfigDirectoryPathAsync();
    Task<string> GetDataDirectoryPathAsync();
    Task<string> GetJobsDirectoryPathAsync();
    Task<string> GetQuackViewDirectoryAsync();
    Task<string> GetSecretsDirectoryPathAsync();
}

internal class SpecialDirectories(ILogger<SpecialDirectories> logger, IDirectoryService directory) : ISpecialDirectories
{
    public const string SecretsDirectoryName = "secrets";
    public const string DataDirectoryName = "data";
    public const string ConfigDirectoryName = "config";
    public const string JobsDirectoryName = "jobs";

    private readonly ILogger<SpecialDirectories> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));

    public virtual async Task<string> GetQuackViewDirectoryAsync()
    {
        return await EnsureExistsAndReturn(Environment.GetEnvironmentVariable("QUACKVIEW_DIR") ?? string.Empty);
    }

    public virtual async Task<string> GetSecretsDirectoryPathAsync()
    {
        return await EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialDirectories.SecretsDirectoryName));
    }

    public virtual async Task<string> GetDataDirectoryPathAsync()
    {
        return await EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialDirectories.DataDirectoryName));
    }

    public virtual async Task<string> GetConfigDirectoryPathAsync()
    {
        return await EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialDirectories.ConfigDirectoryName));
    }

    public virtual async Task<string> GetJobsDirectoryPathAsync()
    {
        return await EnsureExistsAndReturn(Path.Combine(await GetQuackViewDirectoryAsync(), SpecialDirectories.JobsDirectoryName));
    }
    
    protected virtual async Task<string> EnsureExistsAndReturn(string directory)
    {
        await this.directory.CreateDirectoryAsync(directory);

        return directory;
    }
}