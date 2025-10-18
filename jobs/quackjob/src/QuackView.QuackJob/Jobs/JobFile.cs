using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class JobFile
{
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JobMetadata Metadata { get; set; } = new();

    public JsonObject? Config { get; set; } = null;

    public string ToJson(JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(this, options ?? JobFile.DefaultJsonSerializerOptions);

        return json;
    }
}

internal class JobFile<TConfig> : JobFile
{
    public new TConfig? Config { get; set; } = default!;

    public static JobFile<TConfig> FromJobFile(JobFile jobFile)
    {
        return new JobFile<TConfig>
        {
            Metadata = jobFile.Metadata,
            Config = jobFile.Config is TConfig config ? config : default
        };
    }

    // public new string ToJson(JsonSerializerOptions? options = null)
    // {
    //     var json = JsonSerializer.Serialize(this, options ?? JobFile.DefaultJsonSerializerOptions);

    //     return json;
    // }
}

internal class JobMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Runner { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
}