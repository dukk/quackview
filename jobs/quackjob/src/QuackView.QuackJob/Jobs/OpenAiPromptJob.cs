using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class OpenAiPromptJob(
    ILogger<OpenAiPromptJob> logger,
    IDataFileService dataFileService,
    IConsoleService console) 
    : JobRunner
{
    protected readonly ILogger<OpenAiPromptJob> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDataFileService DataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));

    public override async Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        var config = this.LoadJsonConfig<OpenAiPromptJobConfig>(jsonConfig)
            ?? throw new ArgumentException("Invalid job configuration.", nameof(jsonConfig));

        if (string.IsNullOrWhiteSpace(config.Prompt))
            throw new ArgumentNullException(nameof(config), "Invalid job configuration.");

        this.Console.WriteLine("Executing OpenAI Prompt job.");

        var apiKey = config.ApiKey ??Environment.GetEnvironmentVariable("OPENAI_API_KEY");

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        OpenAIResponseClient client = new(model: config.Model, apiKey: apiKey);
        OpenAIResponse response = await client.CreateResponseAsync(config.Prompt,
            options: new ResponseCreationOptions()
            {
                Instructions = "Response must be a valid JSON object."
            });
        var responseText = response.GetOutputText();

        this.Console.WriteLine($"Writing output file: {config.OutputFileName}");
        await this.DataFileService.WriteFileAsync(config.OutputFileName, responseText);

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}

internal class OpenAiPromptJobConfig()
{
    public string Prompt { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4.1";

    public string? ApiKey { get; set; }

    public string OutputFileName { get; set; } = "openai/prompt-output.json";
}