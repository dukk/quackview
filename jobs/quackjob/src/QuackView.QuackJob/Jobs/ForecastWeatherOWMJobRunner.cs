// using System.Text.Json;
// using Microsoft.Extensions.Logging;
// using OpenAI;
// using OpenAI.Responses;
// using TypoDukk.QuackView.QuackJob.Data;
// using TypoDukk.QuackView.QuackJob.Services;

// namespace TypoDukk.QuackView.QuackJob.Jobs;

// [JobRunner("open-weather-map-weather-forecast", "")]
// internal class ForecastWeatherOWMJobRunner(
//     ILogger<ForecastWeatherOWMJobRunner> logger,
//     IFileService file,
//     IDataFileService dataFile,
//     IConsoleService console,
//     ISecretStore secretStore)
//     : JobRunner<JobFile<ForecastWeatherOWMJobRunner>>(file)
// {
//     protected readonly ILogger<ForecastWeatherOWMJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     protected readonly IDataFileService DataFile = dataFile ?? throw new ArgumentNullException(nameof(dataFile));
//     protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));
//     protected readonly ISecretStore SecretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));

//     public override async Task ExecuteJobFileAsync(JobFile<ForecastWeatherOpenWeatherMapConfig> jobFile)
//     {
//         throw new NotImplementedException();

//         if (null == jobFile.Config)
//             throw new ArgumentException("Invalid job configuration.", nameof(jobFile));

//         if (string.IsNullOrWhiteSpace(jobFile.Config.ApiKey))
//             throw new ArgumentException("Invalid job configuration. ApiKey is required.", nameof(jobFile));

//         if (string.IsNullOrWhiteSpace(jobFile.Config.Location))
//             throw new ArgumentException("Invalid job configuration. Location is required.", nameof(jobFile));

//         var apiKey = await this.SecretStore.ExpandSecretsAsync(jobFile.Config.ApiKey);
//         var location = Uri.EscapeDataString(jobFile.Config.Location);
//         var units = Uri.EscapeDataString(jobFile.Config.Units ?? "imperial");

//         // http://api.openweathermap.org/geo/1.0/direct?q={city name},{state code},{country code}&limit={limit}&appid={API key}

//         var url = $"https://api.openweathermap.org/data/2.5/forecast?lat={lat}&lon={lon}&appid={apiKey}";

//         using var client = new HttpClient();


//         this.Console.WriteLine($"Fetching current weather data from OpenWeatherMap.org for location '{location}'");
//         this.Logger.LogDebug("Calling URL: {Url}", url);

//         var response = await client.GetAsync(url);

//         if (response.IsSuccessStatusCode)
//         {
//             if (this.Logger.IsEnabled(LogLevel.Debug))
//             {
//                 var responseBody = await response.Content.ReadAsStringAsync();
//                 this.Logger.LogDebug("Raw Response:{NewLine}{ResponseBody}", Environment.NewLine, responseBody);
//             }

//             var jsonDocument = await JsonDocument.ParseAsync(response.Content.ReadAsStream());


//         }
//         else
//         {
//             Console.WriteLine($"Error: {response.StatusCode}");
//         }
//     }
// }

// internal class ForecastWeatherOpenWeatherMapConfig : FileOutputJobConfig
// {
//     public ForecastWeatherOpenWeatherMapConfig() : base()
//     {
//         this.OutputDataFilePath = "weather/forecast-weather.json";
//     }

//     public string ApiKey { get; set; } = string.Empty;

//     public string Location { get; set; } = string.Empty;

//     public string Units { get; set; } = "imperial";
// }