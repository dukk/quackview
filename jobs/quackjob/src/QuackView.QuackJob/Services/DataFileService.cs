using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDataFileService
{
    Task<bool> ExistsAsync(string path);

    Task WriteFileAsync(string path, string content);

    Task WriteJsonFileAsync<T>(string path, T content);

    Task AppendToFileAsync(string path, string content);

    Task AppendToJsonListFileAsync<T>(string path, T content);

    Task DeleteFileAsync(string path);

    Task DeleteAllJsonListItemsAsync<T>(string path);

    Task DeleteJsonListItemsAsync<T>(string path, Func<T, bool> itemsToDelete);

    Task<string> ReadAllTextAsync(string path);

    Task<JsonListFile<T>> ReadJsonListFileAsync<T>(string path);
}

internal class DataFileService(
    ILogger<DataFileService> logger,
    IDiskIOService disk,
    IDataDirectoryService dataDirectory,
    ISpecialPaths specialPaths)
    : IDataFileService
{
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = Program.DefaultJsonSerializerOptions;

    protected readonly ILogger<DataFileService> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDiskIOService Disk = disk ?? throw new ArgumentNullException(nameof(disk));
    protected readonly IDataDirectoryService DataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
    protected readonly ISpecialPaths SpecialPaths = specialPaths ?? throw new ArgumentNullException(nameof(specialPaths));

    public async Task<bool> ExistsAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = await this.GetFullPathAsync(path);

        return await this.Disk.FileExistsAsync(path);
    }

    public async Task WriteJsonFileAsync<T>(string path, T content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        var json = JsonSerializer.Serialize(content, DataFileService.DefaultJsonSerializerOptions);

        await this.WriteFileAsync(path, json);
    }

    public async Task WriteFileAsync(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        try
        {
            var fullPath = await this.GetFullPathAsync(path);
            var directory = Path.GetDirectoryName(fullPath);

            if (directory != null && !await this.Disk.DirectoryExistsAsync(directory))
                await this.Disk.CreateDirectoryAsync(directory);

            await this.Disk.WriteAllTextAsync(fullPath, content);
            logger.LogInformation("Data file written to {fullPath}", fullPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to write data file to {path}", ex);
        }
    }

    public async Task AppendToFileAsync(string path, string content)
    {
        // TODO: Add file locking

        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        try
        {
            path = await this.GetFullPathAsync(path);

            await this.Disk.AppendAllTextAsync(path, content);
            this.Logger.LogInformation("Appended line to {Path}", path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to append data file to {path}", ex);
        }
    }

    public async Task<JsonListFile<T>> ReadJsonListFileAsync<T>(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = await this.GetFullPathAsync(path);

        try
        {
            var json = await this.Disk.ReadAllTextAsync(path);
            var jsonList = JsonSerializer.Deserialize<JsonListFile<T>>(json, DataFileService.DefaultJsonSerializerOptions);

            return jsonList ?? throw new InvalidOperationException("Failed to deserialize JSON list file.");
        }
        catch (Exception exception)
        {
            throw new FileLoadException("Failed to load JSON list file.", exception);
        }
    }

    public async Task DeleteAllJsonListItemsAsync<T>(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = await this.GetFullPathAsync(path);

        try
        {
            var json = await this.Disk.ReadAllTextAsync(path);
            var jsonList = JsonSerializer.Deserialize<JsonListFile<T>>(json, DataFileService.DefaultJsonSerializerOptions);

            if (jsonList != null)
            {
                jsonList.List = [];
                jsonList.Metadata.LastUpdated = DateTime.UtcNow;

                var updatedJson = JsonSerializer.Serialize(jsonList, DataFileService.DefaultJsonSerializerOptions);

                await this.Disk.WriteAllTextAsync(path, updatedJson);
            }
        }
        catch (Exception exception)
        {
            throw new FileLoadException("Failed to load JSON list file.", exception);
        }
    }

    public async Task DeleteJsonListItemsAsync<T>(string path, Func<T, bool> itemsToDelete)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(itemsToDelete);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = await this.GetFullPathAsync(path);

        try
        {
            var json = await this.Disk.ReadAllTextAsync(path);
            var jsonList = JsonSerializer.Deserialize<JsonListFile<T>>(json, DataFileService.DefaultJsonSerializerOptions);

            if (jsonList != null)
            {
                jsonList.List = [.. jsonList.List.Where(item => !itemsToDelete(item))];
                jsonList.Metadata.LastUpdated = DateTime.UtcNow;

                var updatedJson = JsonSerializer.Serialize(jsonList, DataFileService.DefaultJsonSerializerOptions);

                await this.Disk.WriteAllTextAsync(path, updatedJson);
            }
        }
        catch (Exception exception)
        {
            throw new FileLoadException("Failed to load JSON list file.", exception);
        }
    }

    public async Task AppendToJsonListFileAsync<T>(string path, T content)
    {
        // TODO: Add file locking

        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        try
        {
            path = await this.GetFullPathAsync(path);

            JsonListFile<T>? jsonArrayFile = null;

            if (await this.Disk.FileExistsAsync(path))
            {
                var existingContent = await this.Disk.ReadAllTextAsync(path);
                if (!string.IsNullOrWhiteSpace(existingContent))
                {
                    try
                    {
                        jsonArrayFile = JsonSerializer.Deserialize<JsonListFile<T>>(
                            existingContent, DataFileService.DefaultJsonSerializerOptions) ?? new JsonListFile<T>();
                    }
                    catch (JsonException jsonEx)
                    {
                        this.Logger.LogError(jsonEx, "Failed to deserialize existing JSON content in {Path}. The file may be corrupted or not contain a JSON array.", path);
                        throw;
                    }
                }
            }
            else
            {
                jsonArrayFile = new JsonListFile<T>();
            }

            jsonArrayFile!.List.Add(content);
            jsonArrayFile!.Metadata.LastUpdated = DateTime.UtcNow;

            var json = JsonSerializer.Serialize(jsonArrayFile, DataFileService.DefaultJsonSerializerOptions);
            await Disk.WriteAllTextAsync(path, json);

            this.Logger.LogInformation("Appended item to JSON array file at {Path}", path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to append to JSON array file at {path}", ex);
        }
    }

    public async Task DeleteFileAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        try
        {
            path = await this.GetFullPathAsync(path);

            if (await this.Disk.FileExistsAsync(path))
            {
                await this.Disk.DeleteFileAsync(path);
                this.Logger.LogInformation("Deleted file {Path}", path);
            }
            else
            {
                this.Logger.LogWarning("File {Path} does not exist, cannot delete", path);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete data file at {path}", ex);
        }
    }

    protected virtual async Task<string> GetFullPathAsync(string path)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative", nameof(path));

        return Path.Combine(await this.SpecialPaths.GetDataDirectoryPathAsync(), path);
    }

    public async Task<string> ReadAllTextAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        try
        {
            path = await this.GetFullPathAsync(path);
            return await this.Disk.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read data file at {path}", ex);
        }
    }
}