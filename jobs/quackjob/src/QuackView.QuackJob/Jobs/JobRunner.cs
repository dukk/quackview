using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.N;
using Microsoft.Graph.Models;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal interface IJobRunner
{
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(string? configFile = null, IDictionary<string, string>? parsedArgs = null);
}

internal abstract partial class JobRunner : IJobRunner
{
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    [GeneratedRegex("([A-Z])")]
    private static partial Regex nameRegex();

    public virtual string Name => JobRunner.GetNameFromType(this.GetType());

    public string Description { get; protected set; } = "No description available.";

    internal static string GetNameFromType(Type type)
    {
        return JobRunner.GetNameFromType(type.Name);
    }

    internal static string GetNameFromType(string typeName)
    {
        const string ending = "job";
        var name = nameRegex().Replace(typeName, "-$1").ToLower();
        if (name.EndsWith(ending))
            name = name[..^3];
        return name.Trim('-');
    }

    public abstract Task ExecuteAsync(string? configFile = null, IDictionary<string, string>? parsedArgs = null);

    protected virtual async Task<TConfig> LoadJsonConfigAsync<TConfig>(string? configFile, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(configFile))
            throw new ArgumentNullException(nameof(configFile));

        if (!File.Exists(configFile))
            throw new FileNotFoundException($"Config file '{configFile}' not found.", configFile);

        options ??= DefaultJsonSerializerOptions;

        var configJson = await File.ReadAllTextAsync(configFile);
        var config = JsonSerializer.Deserialize<TConfig>(configJson, options)
            ?? throw new InvalidOperationException($"Failed to deserialize config file '{configFile}' to type '{typeof(TConfig).FullName}'.");

        return config;
    }
}