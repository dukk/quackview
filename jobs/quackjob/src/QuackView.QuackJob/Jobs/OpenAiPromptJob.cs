using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class OpenAiPromptJob(ILogger<OpenAiPromptJob> logger, IDataFileService dataFileService) : Job<OpenAiPromptJobConfig>
{
    private readonly ILogger<OpenAiPromptJob> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));

    public override async Task ExecuteAsync(OpenAiPromptJobConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.Prompt))
            throw new ArgumentNullException(nameof(config), "Invalid job configuration.");

        logger.LogInformation("Executing OpenAI Prompt job.");

        var apiKey = config.ApiKey ??Environment.GetEnvironmentVariable("OPENAI_API_KEY");

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        OpenAIResponseClient client = new(model: config.Model, apiKey: apiKey);
        OpenAIResponse response = await client.CreateResponseAsync(config.Prompt,
            options: new ResponseCreationOptions()
            {
                Instructions = "Response must be a valid JSON object."
            });
        var responseText = response.GetOutputText();

        await this.dataFileService.WriteFile(config.OutputFileName, responseText);

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