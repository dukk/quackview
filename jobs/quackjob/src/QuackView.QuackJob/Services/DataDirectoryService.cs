using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDataDirectoryService
{
    Task<bool> ExistsAsync(string path);
    Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false);
}

internal class DataDirectoryService(ILogger<DataDirectoryService> logger, IDirectoryService directory) : IDataDirectoryService
{
    private readonly ILogger<DataDirectoryService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));

    public async Task<bool> ExistsAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        return await directory.ExistsAsync(path);
    }

    public async Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(searchPattern);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        return await directory.EnumerateFilesAsync(path, searchPattern, includeSubdirectories);
    }
}