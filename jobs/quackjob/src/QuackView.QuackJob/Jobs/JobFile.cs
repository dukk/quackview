using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class JobFile // TODO: Refactor this, it's messy...
{
    public JobMetadata Metadata { get; set; } = new();

    public virtual string ToJson(JsonSerializerOptions? options = null)
    {
         var json = JsonSerializer.Serialize<JobFile>(this, options ?? Program.DefaultJsonSerializerOptions);

        return json;
    }
}

internal class JobFile<TConfig> : JobFile
{
    public TConfig? Config { get; set; } = default!;
    
    public new string ToJson(JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize<JobFile<TConfig>>(this, options ?? Program.DefaultJsonSerializerOptions);

        return json;
    }
}

internal class JobMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Runner { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
}