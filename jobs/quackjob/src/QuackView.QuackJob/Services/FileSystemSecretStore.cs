using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface ISecretStore
{
    Task<string> GetSecretAsync(string key);
    Task SetSecretAsync(string key, string value, bool overwrite = false);
    Task<string> ExpandSecretsAsync(string input);
}

internal partial class FileSystemSecretStore(ILogger<FileSystemSecretStore> logger, IDirectoryService directory, IFileService file) : ISecretStore
{
    // This isn't great security but it's better than nothing...

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    public static partial Regex KeyValidationRegex();

    public static bool IsInvalidKey(string key) => !KeyValidationRegex().IsMatch(key);
    public static void ThrowIfInvalidKey(string key)
    {
        if (IsInvalidKey(key)) 
            throw new ArgumentException("Key contains invalid characters. Only alphanumeric characters, underscores, and hyphens are allowed.", nameof(key));
    }

    private readonly ILogger<FileSystemSecretStore> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDirectoryService directory = directory ?? throw new ArgumentNullException(nameof(directory));
    private readonly IFileService file = file ?? throw new ArgumentNullException(nameof(file));

    public async Task<string> GetSecretAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        FileSystemSecretStore.ThrowIfInvalidKey(key);
        
        this.logger.LogDebug("Retrieving secret for key: {Key}", key);

        var secretFilePath = Path.Combine(await this.GetSecretsDirectoryPathAsync(), $"{key}.secret");

        if (!await this.file.ExistsAsync(secretFilePath))
            throw new KeyNotFoundException("Unknown secret.");

        return await this.file.ReadAllTextAsync(secretFilePath);
    }

    public async Task SetSecretAsync(string key, string value, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        FileSystemSecretStore.ThrowIfInvalidKey(key);

        this.logger.LogDebug("Setting secret for key: {Key}", key);

        var secretFilePath = Path.Combine(await this.GetSecretsDirectoryPathAsync(), $"{key}.secret");

        if (!overwrite && await this.file.ExistsAsync(secretFilePath))
            throw new ArgumentException("Secret already exists and overwrite is set to false.", nameof(key));

        await this.file.WriteAllTextAsync(secretFilePath, value);
    }

    public async Task<string> ExpandSecretsAsync(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var files = await this.directory.EnumerateFilesAsync(await this.GetSecretsDirectoryPathAsync());

        foreach (var file in files)
        {
            var secretName = Path.GetFileNameWithoutExtension(file);
            var secretValue = await this.file.ReadAllTextAsync(file);

            input = input.Replace($"$^{{{secretName}}}", secretValue, StringComparison.OrdinalIgnoreCase);
        }

        if (input.Contains("$^{"))
            throw new KeyNotFoundException("One or more secrets in the input string were not found in the secret store.");

        return input;
    }

    protected virtual async Task<string> GetSecretsDirectoryPathAsync()
    {
        var path = Environment.GetEnvironmentVariable("QUACKVIEW_DIR") ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(path))
            path = Path.Combine(path, "secrets");

        await this.directory.CreateDirectoryAsync(path);

        this.logger.LogDebug("Using secrets directory path: {Path}", path);

        return path;
    }
}