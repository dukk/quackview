using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Data;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Jobs;

[JobRunner("open-weather-map-current-weather", "")]
internal class CurrentWeatherOWMJobRunner(
    ILogger<CurrentWeatherOWMJobRunner> logger,
    IDiskIOService disk,
    IDataFileService dataFile,
    IConsoleService console,
    ISecretStore secretStore)
    : JobRunner<JobFile<CurrentWeatherOpenWeatherMapConfig>>(disk)
{
    protected readonly ILogger<CurrentWeatherOWMJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDataFileService DataFile = dataFile ?? throw new ArgumentNullException(nameof(dataFile));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));
    protected readonly ISecretStore SecretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));

    public override async Task ExecuteJobFileAsync(JobFile<CurrentWeatherOpenWeatherMapConfig> jobFile)
    {
        if (null == jobFile.Config)
            throw new ArgumentException("Invalid job configuration.", nameof(jobFile));

        if (string.IsNullOrWhiteSpace(jobFile.Config.ApiKey))
            throw new ArgumentException("Invalid job configuration. ApiKey is required.", nameof(jobFile));

        if (string.IsNullOrWhiteSpace(jobFile.Config.Location))
            throw new ArgumentException("Invalid job configuration. Location is required.", nameof(jobFile));

        var apiKey = await this.SecretStore.ExpandSecretsAsync(jobFile.Config.ApiKey);
        var location = Uri.EscapeDataString(jobFile.Config.Location);
        var units = Uri.EscapeDataString(jobFile.Config.Units ?? "imperial");
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={location}&units={units}&appid={apiKey}";

        using var client = new HttpClient();


        this.Console.WriteLine($"Fetching current weather data from OpenWeatherMap.org for location '{location}'");
        this.Logger.LogDebug("Calling URL: {Url}", url);

        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            if (this.Logger.IsEnabled(LogLevel.Debug))
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                this.Logger.LogDebug("Raw Response:{NewLine}{ResponseBody}", Environment.NewLine, responseBody);
            }

            var jsonDocument = await JsonDocument.ParseAsync(response.Content.ReadAsStream());

            jsonDocument.RootElement.TryGetProperty("dt", out var dt);
            jsonDocument.RootElement.TryGetProperty("main", out var main);
            jsonDocument.RootElement.TryGetProperty("wind", out var wind);
            jsonDocument.RootElement.TryGetProperty("weather", out var weatherArray);

            var weatherDescription = weatherArray[0].GetProperty("description").GetString();
            var weather = new CurrentWeatherInfo
            {
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(dt.GetInt64()),
                Temperature = new DoubleUnitValue
                {
                    Value = main.GetProperty("temp").GetDouble(),
                    Unit = "°F"
                },
                FeelsLikeTemperature = new DoubleUnitValue
                {
                    Value = main.GetProperty("feels_like").GetDouble(),
                    Unit = "°F"
                },
                Humidity = new IntUnitValue
                {
                    Value = main.GetProperty("humidity").GetInt32(),
                    Unit = "%"
                },
                Wind = new WindInfo
                {
                    Speed = new DoubleUnitValue
                    {
                        Value = wind.GetProperty("speed").GetDouble(),
                        Unit = "mph"
                    },
                    Direction = wind.GetProperty("deg").GetInt32() + "°"
                },
                Summary = weatherDescription ?? string.Empty
            };

            await this.DataFile.WriteJsonFileAsync(jobFile.Config.OutputDataFilePath, weather);
        }
        else
        {
            Console.WriteLine($"Error: {response.StatusCode}");
        }
    }
}

internal class CurrentWeatherOpenWeatherMapConfig : FileOutputJobConfig
{
    public CurrentWeatherOpenWeatherMapConfig() : base()
    {
        this.OutputDataFilePath = "weather/current-weather.json";
    }

    public string ApiKey { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Units { get; set; } = "imperial";
}