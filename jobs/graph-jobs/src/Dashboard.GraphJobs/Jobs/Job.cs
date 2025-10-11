using System.Text.Json;

namespace TypoDukk.Dashboard.GraphJobs.Jobs;

internal interface IJob
{
    bool MatchesArgName(string argName);
    Task ExecuteFromConfigFileAsync(string configFile);
}

internal interface IJob<TConfig> : IJob
    where TConfig : class, new()
{
    Task ExecuteAsync(TConfig config);
}

internal abstract class Job<TConfig> : IJob<TConfig>, IJob
    where TConfig : class, new()
{
    bool IJob.MatchesArgName(string argName)
    {
        var name = this.GetType().Name;

        if (name.EndsWith("Job", StringComparison.InvariantCultureIgnoreCase))
            name = name[..^3];

        return name.Equals(argName, StringComparison.InvariantCultureIgnoreCase)
            || name.Replace("-", "").Equals(argName, StringComparison.InvariantCultureIgnoreCase);
    }

    public abstract Task ExecuteAsync(TConfig config);

    async Task IJob.ExecuteFromConfigFileAsync(string configFile)
    {
        if (string.IsNullOrWhiteSpace(configFile))
            throw new ArgumentNullException(nameof(configFile));

        if (!File.Exists(configFile))
            throw new FileNotFoundException($"Config file '{configFile}' not found.", configFile);

        var configJson = await File.ReadAllTextAsync(configFile);
        var config = JsonSerializer.Deserialize<TConfig>(configJson) 
            ?? throw new InvalidOperationException($"Failed to deserialize config file '{configFile}' to type '{typeof(TConfig).FullName}'.");
        
        await this.ExecuteAsync(config);
    }
}