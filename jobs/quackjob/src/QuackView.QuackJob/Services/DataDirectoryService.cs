using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IDataDirectoryService
{
    string GetDataDirectoryPath();
    bool Exists(string path);

    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool includeSubdirectories = false);
}

internal class DataDirectoryService(ILogger<DataDirectoryService> logger, IDirectoryService directory) : IDataDirectoryService
{
    private readonly ILogger<DataDirectoryService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));

    public bool Exists(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        return directory.Exists(path);
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool includeSubdirectories = false)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(searchPattern);
        
        if (Path.IsPathRooted(path))
            throw new ArgumentException("Path must be relative.", nameof(path));

        return directory.EnumerateFiles(path, searchPattern, includeSubdirectories);
    }

    public string GetDataDirectoryPath()
    {
        var dataDir = Environment.GetEnvironmentVariable("QUACKVIEW_DIR") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(dataDir))
            dataDir = Path.Combine(dataDir, "data");

        if (string.IsNullOrEmpty(dataDir))
            dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuackView", "QuackJob", "Data");

        directory.CreateDirectory(dataDir);

        return dataDir;
    }
}