using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Jobs;

[JobRunner("open-ai-prompt", "")]
internal class OpenAiPromptJobRunner(
    ILogger<OpenAiPromptJobRunner> logger,
    IDataFileService dataFileService,
    IConsoleService console,
    ISecretStore secretStore,
    IDiskIOService file)
    : JobRunner<JobFile<OpenAiPromptJobConfig>>(file)
{
    protected readonly ILogger<OpenAiPromptJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDataFileService DataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));
    protected readonly ISecretStore SecretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));

    public override async Task ExecuteJobFileAsync(JobFile<OpenAiPromptJobConfig> jobFile)
    {
        var config = jobFile.Config ?? throw new ArgumentException("Invalid job configuration.", nameof(jobFile));

        if (config.Prompt == null || config.Prompt.Count == 0)
            throw new ArgumentNullException(nameof(config), "Invalid job configuration.");

        this.Console.WriteLine("Executing OpenAI Prompt job.");

        var rawApiKey = string.IsNullOrWhiteSpace(config.ApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            : config.ApiKey;

        if (string.IsNullOrWhiteSpace(rawApiKey))
            throw new InvalidOperationException("OpenAI API key is missing. Provide 'ApiKey' in job config or set the 'OPENAI_API_KEY' environment variable.");

        var apiKey = await this.SecretStore.ExpandSecretsAsync(rawApiKey!);

        var fullPrompt = string.Join(Environment.NewLine, config.Prompt);

        if (string.IsNullOrWhiteSpace(fullPrompt))
            throw new ArgumentNullException(nameof(config.Prompt), "Invalid job configuration. Prompt is empty.");

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var client = new OpenAIResponseClient(model: config.Model, apiKey: apiKey);
        var result = await client.CreateResponseAsync(fullPrompt, new ResponseCreationOptions()
        {
            ParallelToolCallsEnabled = true
        });
        var response = result.Value;
        var responseText = response.GetOutputText();

        this.Console.WriteLine($"Writing output file: {config.OutputDataFilePath}");
        await this.DataFileService.WriteFileAsync(config.OutputDataFilePath, responseText);

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    public override async Task CreateNewJobFileAsync(string filePath)
    {
        var content = JsonSerializer.Serialize(new JobFile<OpenAiPromptJobConfig>()
        {
            Metadata = new()
            {
                Name = "Open AI Prompt",
                Description = "Generate json files based on a prompt for Open AI prompt",
                Runner = "open-ai-prompt",
                Schedule = "* * * * *"
            },
            Config = new()
            {
                Prompt = ["Give me a list of US presidents in a JSON array."],
                ApiKey = "$^{open-ai-api-key}"
            }
        }, options: Program.DefaultJsonSerializerOptions);

        await this.Disk.AppendAllTextAsync(filePath, content);
    }
}

internal class OpenAiPromptJobConfig : FileOutputJobConfig
{
    public OpenAiPromptJobConfig() : base()
    {
        this.OutputDataFilePath = "openai-prompt-output.json";
    }

    public List<string> Prompt { get; set; } = [];

    //public List<string> Instructions { get; set; } = [];

    public string Model { get; set; } = "gpt-4.1";

    public string? ApiKey { get; set; }
}