using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

// [Action(shorthand: "h")]
internal class HelpAction(ILogger<HelpAction> logger, IConsoleService console) : Action(logger, console)
{
    private readonly new ILogger<HelpAction> Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override async Task ExecuteAsync(string[] args)
    {
        var assembly = typeof(HelpAction).Assembly;
        using var stream = assembly.GetManifestResourceStream("HelpText") ?? throw new Exception("Help documentation not found.");
        using var reader = new StreamReader(stream);
        var helpText = await reader.ReadToEndAsync();

        this.Console.WriteLine(helpText);

        return;
    }
}