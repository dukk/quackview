using System.Diagnostics.CodeAnalysis;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDiskIOService
{
    Task CopyAsync(string sourcePath, string destinationPath);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string content);
    Task AppendAllTextAsync(string path, string content);
    Task<bool> FileExistsAsync(string path);
    Task DeleteFileAsync(string path);
    Task MoveFileAsync(string sourcePath, string destinationPath);
    Task CreateDirectoryAsync(string path);
    Task<bool> DirectoryExistsAsync(string path);
    Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false);
}

[ExcludeFromCodeCoverage]
internal class DiskIOService : IDiskIOService
{
    public Task CopyAsync(string sourcePath, string destinationPath)
    {
        return Task.Run(() =>
        {
            File.Copy(sourcePath, destinationPath, true);
        });
    }
    public Task<string> ReadAllTextAsync(string path)
    {
        return File.ReadAllTextAsync(path);
    }

    public Task WriteAllText(string path, string content)
    {
        return File.WriteAllTextAsync(path, content);
    }

    public Task AppendAllTextAsync(string path, string content)
    {
        return File.AppendAllTextAsync(path, content);
    }

    public Task<bool> FileExistsAsync(string path)
    {
        return Task.FromResult(File.Exists(path));
    }

    public Task DeleteFileAsync(string path)
    {
        return Task.Run(() =>
        {
            File.Delete(path);
        });
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath)
    {
        return Task.Run(() =>
        {
            File.Move(sourcePath, destinationPath);
        });
    }

    public Task WriteAllTextAsync(string path, string content)
    {
        return File.WriteAllTextAsync(path, content);
    }

    public async Task CreateDirectoryAsync(string path)
    {
        await Task.Run(() => Directory.CreateDirectory(path));
    }

    public async Task<bool> DirectoryExistsAsync(string path)
    {
        return await Task.Run(() => Directory.Exists(path));
    }

    public Task<IEnumerable<string>> EnumerateFilesAsync(string path, string searchPattern = "*", bool includeSubdirectories = false)
    {
        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Task.Run(() => Directory.EnumerateFiles(path, searchPattern, searchOption));
    }
}