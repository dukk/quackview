namespace TypoDukk.QuackView.QuackJob.Data;

internal class CurrentWeatherInfo
{
    public DateTimeOffset Timestamp { get; set; }
    public DoubleUnitValue Temperature { get; set; } = new DoubleUnitValue();
    public DoubleUnitValue FeelsLikeTemperature { get; set; } = new DoubleUnitValue();
    public IntUnitValue Humidity { get; set; } = new IntUnitValue();
    public WindInfo Wind { get; set; } = new WindInfo();
    public string Summary { get; set; } = string.Empty;
}

internal class WindInfo
{
    public DoubleUnitValue Speed { get; set; } = new DoubleUnitValue();
    public string Direction { get; set; } = string.Empty;
}

internal class DoubleUnitValue
{
    public double Value { get; set; } = 0.0;
    public string Unit { get; set; } = string.Empty;
}

internal class IntUnitValue
{
    public int Value { get; set; } = 0;
    public string Unit { get; set; } = string.Empty;
}