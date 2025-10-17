using System.Reflection;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class HelpAction(ILogger<HelpAction> logger, IConsoleService console) : Action(console)
{
    private readonly ILogger<HelpAction> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override async Task ExecuteAsync(string[] args)
    {
        var assembly = typeof(HelpAction).Assembly;
        using var stream = assembly.GetManifestResourceStream("HelpText") ?? throw new Exception("Help documentation not found.");
        using var reader = new StreamReader(stream);
        var helpText = await reader.ReadToEndAsync();
        
        console.WriteLine(helpText);

        return;
    }
}