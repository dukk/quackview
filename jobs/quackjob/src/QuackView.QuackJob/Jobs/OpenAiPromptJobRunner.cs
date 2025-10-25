using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Jobs;

[JobRunner("open-ai-prompt", "")]
internal class OpenAiPromptJobRunner(
    ILogger<OpenAiPromptJobRunner> logger,
    IDataFileService dataFileService,
    IConsoleService console,
    ISecretStore secretStore,
    IFileService file)
    : JobRunner<JobFile<OpenAiPromptJobConfig>>(file)
{
    protected readonly ILogger<OpenAiPromptJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDataFileService DataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));
    protected readonly ISecretStore SecretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));

    public override async Task ExecuteJobFileAsync(JobFile<OpenAiPromptJobConfig> jobFile)
    {
        var config = jobFile.Config ?? throw new ArgumentException("Invalid job configuration.", nameof(jobFile));

        if (string.IsNullOrWhiteSpace(config.Prompt))
            throw new ArgumentNullException(nameof(config), "Invalid job configuration.");

        this.Console.WriteLine("Executing OpenAI Prompt job.");

        var rawApiKey = string.IsNullOrWhiteSpace(config.ApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            : config.ApiKey;

        if (string.IsNullOrWhiteSpace(rawApiKey))
            throw new InvalidOperationException("OpenAI API key is missing. Provide 'ApiKey' in job config or set the 'OPENAI_API_KEY' environment variable.");

        var apiKey = await this.SecretStore.ExpandSecretsAsync(rawApiKey!);

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        OpenAIResponseClient client = new(model: config.Model, apiKey: apiKey);
        OpenAIResponse response = await client.CreateResponseAsync(config.Prompt,
            options: new ResponseCreationOptions()
            {
                Instructions = "Response must be a valid JSON object."
            });
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
                Prompt = "Give me a list of US presidents in a JSON array.",
                ApiKey = "$^{open-ai-api-key}"
            }
        }, options: Program.DefaultJsonSerializerOptions);

        await this.File.AppendAllTextAsync(filePath, content);
    }
}

internal class OpenAiPromptJobConfig : FileOutputJobConfig
{
    public OpenAiPromptJobConfig() : base()
    {
        this.OutputDataFilePath = "openai-prompt-output.json";
    }

    public string Prompt { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4.1";

    public string? ApiKey { get; set; }
}