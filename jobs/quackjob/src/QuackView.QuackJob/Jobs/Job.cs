using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.N;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal interface IJob
{
    string Name { get; }
    string Description { get; }
    Task ExecuteFromConfigFileAsync(string configFile);
}

internal interface IJob<TConfig> : IJob
    where TConfig : class, new()
{
    Task ExecuteAsync(TConfig config);
}

internal abstract partial class Job<TConfig> : IJob<TConfig>, IJob
    where TConfig : class, new()
{
    [GeneratedRegex("([A-Z])")]
    private static partial Regex nameRegex();

    public virtual string Name => Job<TConfig>.GetNameFromType(this.GetType());

    public string Description { get; protected set; } = "No description available.";

    internal static string GetNameFromType(Type type)
    {
        return Job<TConfig>.GetNameFromType(type.Name);
    }

    internal static string GetNameFromType(string typeName)
    {
        const string ending = "job";
        var name = nameRegex().Replace(typeName, "-$1").ToLower();
        if (name.EndsWith(ending))
            name = name[..^3];
        return name.Trim('-');
    }

    public abstract Task ExecuteAsync(TConfig config);

    async Task IJob.ExecuteFromConfigFileAsync(string configFile)
    {
        if (string.IsNullOrWhiteSpace(configFile))
            throw new ArgumentNullException(nameof(configFile));

        if (!File.Exists(configFile))
            throw new FileNotFoundException($"Config file '{configFile}' not found.", configFile);

        var configJson = await File.ReadAllTextAsync(configFile);
        var config = JsonSerializer.Deserialize<TConfig>(configJson, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        }) ?? throw new InvalidOperationException($"Failed to deserialize config file '{configFile}' to type '{typeof(TConfig).FullName}'.");
        
        await this.ExecuteAsync(config);
    }
}