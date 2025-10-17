using System.Diagnostics.CodeAnalysis;

namespace TypoDukk.QuackView.QuackJob.Services;

internal interface IConsoleService
{
    void WriteLine(string message);
    void WriteError(string message);
}

[ExcludeFromCodeCoverage]
internal class ConsoleService : IConsoleService
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteError(string message)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ForegroundColor = previousColor;
    }
}