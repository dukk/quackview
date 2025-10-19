using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.N;
using Microsoft.Graph.Models;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal interface IJobRunner
{
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(JsonElement? jsonConfig);
}

internal abstract partial class JobRunner : IJobRunner
{
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

    public abstract Task ExecuteAsync(JsonElement? jsonConfig = null);

    protected virtual TConfig LoadJsonConfig<TConfig>(JsonElement? jsonConfig, JsonSerializerOptions? options = null)
    {
        if (!jsonConfig.HasValue)
            throw new ArgumentNullException(nameof(jsonConfig));

        return this.LoadJsonConfig<TConfig>(jsonConfig.Value, options);
    }

    protected virtual TConfig LoadJsonConfig<TConfig>(JsonElement jsonConfig, JsonSerializerOptions? options = null)
    {
        options ??= Program.DefaultJsonSerializerOptions;

        var config = JsonSerializer.Deserialize<TConfig>(jsonConfig, options)
            ?? throw new InvalidOperationException($"Failed to deserialize config JSON.");

        return config;
    }
}