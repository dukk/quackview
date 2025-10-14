using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TypoDukk.Dashboard.GraphJobs.Services;

internal interface IDataFileService
{
    Task WriteFile(string path, string content);
    Task WriteJsonFile<T>(string path, T content);
}

internal class DataFileService : IDataFileService
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<DataFileService> logger;

    public DataFileService(ILogger<DataFileService> logger)
    {
        this.logger = logger;
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
            if (Path.IsPathRooted(path))
                throw new ArgumentException("Path must be relative", nameof(path));
            
            path = Path.Combine(this.getDataDirectory(), path);

            await File.WriteAllTextAsync(path, content);
            logger.LogInformation("Data file written to {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write data file to {Path}", path);
            throw;
        }
    }

    private string getDataDirectory()
    {
        var dataDir = Environment.GetEnvironmentVariable("TDD_GRAPHJOBS_DATA_DIR");

        if (string.IsNullOrEmpty(dataDir))
        {
            dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TDDashboard", "TDDGraphJobs", "Data");
        }
        Directory.CreateDirectory(dataDir);
        return dataDir;
    }
}