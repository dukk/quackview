using System.Diagnostics.CodeAnalysis;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IFileService
{
    Task CopyAsync(string sourcePath, string destinationPath);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string content);
    Task AppendAllTextAsync(string path, string content);
    Task<bool> ExistsAsync(string path);
    Task DeleteFileAsync(string path);
    Task MoveFileAsync(string sourcePath, string destinationPath);
}

[ExcludeFromCodeCoverage]
internal class FileService : IFileService
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

    public Task<bool> ExistsAsync(string path)
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
}