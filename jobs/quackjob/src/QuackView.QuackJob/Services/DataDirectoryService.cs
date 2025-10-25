using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDataDirectoryService
{
    Task<bool> ExistsAsync(string path);
    Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false);
}

internal class DataDirectoryService(ILogger<DataDirectoryService> logger, IDirectoryService directory, ISpecialPaths specialPaths) : IDataDirectoryService
{
    protected readonly ILogger<DataDirectoryService> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDirectoryService Directory = directory ?? throw new ArgumentNullException(nameof(directory));
    protected readonly ISpecialPaths SpecialPaths = specialPaths ?? throw new ArgumentNullException(nameof(specialPaths));

    public async Task<bool> ExistsAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        var resolvedPath = Path.Combine(await this.SpecialPaths.GetDataDirectoryPathAsync(), path);
        return await this.Directory.ExistsAsync(resolvedPath);
    }

    public async Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(searchPattern);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        var dataDir = await this.SpecialPaths.GetDataDirectoryPathAsync();
        var resolvedPath = Path.Combine(dataDir, path);
        var files = await this.Directory.EnumerateFilesAsync(resolvedPath, searchPattern, includeSubdirectories);
        return files.Select(f =>
             f[dataDir.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
}