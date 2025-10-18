using System.Diagnostics.CodeAnalysis;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDirectoryService
{
    Task CreateDirectoryAsync(string path);
    Task<bool> ExistsAsync(string path);
    Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false);
}

[ExcludeFromCodeCoverage]
internal class DirectoryService : IDirectoryService
{
    public async Task CreateDirectoryAsync(string path)
    {
        await Task.Run(() => Directory.CreateDirectory(path));
    }

    public async Task<bool> ExistsAsync(string path)
    {
        return await Task.Run(() => Directory.Exists(path));
    }

    public Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false)
    {
        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Task.Run(() => Directory.EnumerateFiles(path, searchPattern, searchOption));
    }
}