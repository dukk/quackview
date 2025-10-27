using System.Text.Json;
using System.Text.Json.Serialization;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal interface IJobFile
{
    JobMetadata Metadata { get; }

    string ToJson(JsonSerializerOptions? options = null);
}

internal class JobFile : JobFile<object>
{

}

internal class JobFile<TConfig> : IJobFile
    where TConfig : class, new()
{
    [JsonPropertyOrder(1)]
    public JobMetadata Metadata { get; set; } = new();

    [JsonPropertyOrder(2)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TConfig? Config { get; set; } = null;

    public string ToJson(JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize<JobFile<TConfig>>(this, options ?? Program.DefaultJsonSerializerOptions);

        return json;
    }
}

internal class JobMetadata()
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Runner { get; set; } = string.Empty;
    public string Schedule { get; set; } = "* * * * *";
}

// This is just so I can stay consistent on the output file name in the configs, I kept calling it something different in each config...
internal class FileOutputJobConfig()
{
    [JsonPropertyOrder(int.MaxValue)] // Always put it last
    public string OutputDataFilePath { get; set; } = "job-output.json";
}
