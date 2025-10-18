using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDataFileService
{
    Task<bool> ExistsAsync(string path);

    Task WriteFileAsync(string path, string content);

    Task WriteJsonFileAsync<T>(string path, T content);

    Task AppendToFileAsync(string path, string content);

    Task AppendToJsonListFileAsync<T>(string path, T content);

    Task DeleteFileAsync(string path);
}

internal class DataFileService(ILogger<DataFileService> logger, IFileService file, IDataDirectoryService directory) : IDataFileService
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<DataFileService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileService file = file ?? throw new ArgumentNullException(nameof(file));
    private readonly IDataDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));

    public async Task<bool> ExistsAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        path = this.getFullPath(path);

        return await this.file.ExistsAsync(path);
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
            path = this.getFullPath(path);

            await this.file.WriteAllTextAsync(path, content);
            logger.LogInformation("Data file written to {Path}", path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to write data file to {path}", ex);
        }
    }

    public async Task AppendToFileAsync(string path, string content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        try
        {
            path = this.getFullPath(path);

            await this.file.AppendAllTextAsync(path, content);
            this.logger.LogInformation("Appended line to {Path}", path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to append data file to {path}", ex);
        }
    }

    public async Task AppendToJsonListFileAsync<T>(string path, T content)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(content);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        try
        {
            path = this.getFullPath(path);

            JsonListFile<T>? jsonArrayFile = null;
            var existingContent = await this.file.ReadAllTextAsync(path);
            
            if (!string.IsNullOrWhiteSpace(existingContent))
            {
                try
                {
                    jsonArrayFile = JsonSerializer.Deserialize<JsonListFile<T>>(
                        existingContent, DataFileService.DefaultJsonSerializerOptions) ?? new JsonListFile<T>();
                }
                catch (JsonException jsonEx)
                {
                    this.logger.LogError(jsonEx, "Failed to deserialize existing JSON content in {Path}. The file may be corrupted or not contain a JSON array.", path);
                    throw;
                }
            }

            jsonArrayFile?.List.Add(content);

            var json = JsonSerializer.Serialize(jsonArrayFile, DataFileService.DefaultJsonSerializerOptions);
            await File.WriteAllTextAsync(path, json);

            this.logger.LogInformation("Appended item to JSON array file at {Path}", path);
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
            path = this.getFullPath(path);

            if (await this.file.ExistsAsync(path))
            {
                await this.file.DeleteFileAsync(path);
                this.logger.LogInformation("Deleted file {Path}", path);
            }
            else
            {
                this.logger.LogWarning("File {Path} does not exist, cannot delete", path);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete data file at {path}", ex);
        }
    }

    private string getFullPath(string path)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative", nameof(path));

        return Path.Combine(this.directory.GetDataDirectoryPath(), path);
    }
}