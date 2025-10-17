using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDataFileService
{
    Task CreateJsonArrayFile<T>(string path, bool overwrite = true);

    bool Exists(string path);

    Task WriteFile(string path, string content);

    Task WriteJsonFile<T>(string path, T content);

    Task AppendToFile(string path, string content);

    Task AppendToJsonArrayFile<T>(string path, T content);

    Task DeleteFile(string path);
}

internal class JsonArrayFile<T>
{
    public FileMetaData MetaData { get; set; } = new FileMetaData();

    public IList<T> List { get; set; } = new List<T>();
}

internal class FileMetaData
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public IList<string> Sources { get; set; } = new List<string>();
}

internal class DataFileService(ILogger<DataFileService> logger) : IDataFileService
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<DataFileService> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task CreateJsonArrayFile<T>(string path, bool overwrite = true)
    {
        var json = JsonSerializer.Serialize(new JsonArrayFile<T>(), DataFileService.DefaultJsonSerializerOptions);

        if (overwrite && this.Exists(path))
            await this.DeleteFile(path);

        await WriteFile(path, json);
    }

    public bool Exists(string path)
    {
        path = this.getFullPath(path);

        return File.Exists(path);
    }

    public async Task WriteJsonFile<T>(string path, T content)
    {
        var json = JsonSerializer.Serialize(content, DataFileService.DefaultJsonSerializerOptions);

        await WriteFile(path, json);
    }

    public async Task WriteFile(string path, string content)
    {
        try
        {
            path = this.getFullPath(path);

            await File.WriteAllTextAsync(path, content);
            logger.LogInformation("Data file written to {Path}", path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to write data file to {path}", ex);
        }
    }

    public async Task AppendToFile(string path, string content)
    {
        try
        {
            path = this.getFullPath(path);

            await File.AppendAllTextAsync(path, content);
            this.logger.LogInformation("Appended line to {Path}", path);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to append data file to {path}", ex);
        }
    }

    public async Task AppendToJsonArrayFile<T>(string path, T content)
    {
        try
        {
            path = this.getFullPath(path);

            JsonArrayFile<T>? jsonArrayFile = null;
            var existingContent = await File.ReadAllTextAsync(path);
            
            if (!string.IsNullOrWhiteSpace(existingContent))
            {
                try
                {
                    jsonArrayFile = JsonSerializer.Deserialize<JsonArrayFile<T>>(
                        existingContent, DataFileService.DefaultJsonSerializerOptions) ?? new JsonArrayFile<T>();
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

    public async Task DeleteFile(string path)
    {
        await Task.Run(() => // HACK: Tired of the warning message since File.Delete isn't awaitable, but I want to keep the interface async...
        {
            try
            {
                path = this.getFullPath(path);

                if (File.Exists(path))
                {
                    File.Delete(path);
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

            return;
        });
    }

    private string getDataDirectory()
    {
        var dataDir = Environment.GetEnvironmentVariable("QUACKVIEW_DIR") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(dataDir))
            dataDir = Path.Combine(dataDir, "data");

        if (string.IsNullOrEmpty(dataDir))
            dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuackView", "QuackJob", "Data");

        Directory.CreateDirectory(dataDir);

        return dataDir;
    }

    private string getFullPath(string path)
    {
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative", nameof(path));

        return Path.Combine(this.getDataDirectory(), path);
    }
}