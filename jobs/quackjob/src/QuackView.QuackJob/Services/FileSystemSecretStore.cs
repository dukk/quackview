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

internal partial class FileSystemSecretStore(
    ILogger<FileSystemSecretStore> logger,
    IDiskIOService disk,
    ISpecialPaths specialPaths) : ISecretStore
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

    protected readonly ILogger<FileSystemSecretStore> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDiskIOService Disk = disk ?? throw new ArgumentNullException(nameof(disk));
    protected readonly ISpecialPaths SpecialPaths = specialPaths ?? throw new ArgumentNullException(nameof(specialPaths));


    public async Task<string> GetSecretAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        FileSystemSecretStore.ThrowIfInvalidKey(key);

        this.Logger.LogDebug("Retrieving secret for key: {Key}", key);

        var secretFilePath = Path.Combine(await this.SpecialPaths.GetSecretsDirectoryPathAsync(), $"{key}.secret");

        if (!await this.Disk.FileExistsAsync(secretFilePath))
            throw new KeyNotFoundException("Unknown secret.");

        var secretValue = await this.Disk.ReadAllTextAsync(secretFilePath);

        secretValue = secretValue.Trim('\n', '\r', ' ', '\t');

        return secretValue;
    }

    public async Task SetSecretAsync(string key, string value, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        FileSystemSecretStore.ThrowIfInvalidKey(key);

        this.Logger.LogDebug("Setting secret for key: {Key}", key);

        var secretFilePath = Path.Combine(await this.SpecialPaths.GetSecretsDirectoryPathAsync(), $"{key}.secret");

        if (!overwrite && await this.Disk.FileExistsAsync(secretFilePath))
            throw new ArgumentException("Secret already exists and overwrite is set to false.", nameof(key));

        await this.Disk.WriteAllTextAsync(secretFilePath, value);
    }

    public async Task<string> ExpandSecretsAsync(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var files = await this.Disk.EnumerateFilesAsync(await this.SpecialPaths.GetSecretsDirectoryPathAsync());

        foreach (var file in files)
        {
            var secretName = Path.GetFileNameWithoutExtension(file);
            var secretValue = await this.Disk.ReadAllTextAsync(file);

            input = input.Replace($"$^{{{secretName}}}", secretValue, StringComparison.OrdinalIgnoreCase);
        }

        if (input.Contains("$^{"))
            throw new KeyNotFoundException("One or more secrets in the input string were not found in the secret store.");

        return input;
    }
}