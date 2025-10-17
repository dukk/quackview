using System;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal interface IAction
{
    string Name { get; }
    Task ExecuteAsync(string[] args);
    void DisplayHelp();
    bool MatchesActionName(string actionName);
}

internal abstract class Action(IConsoleService console) : IAction
{
    protected IConsoleService console = console ?? throw new ArgumentNullException(nameof(console));

    public virtual string Name
    {
        get
        {
            var name = this.GetType().Name;
            if (name.EndsWith("Action", StringComparison.InvariantCultureIgnoreCase))
                name = name[..^6];

            return name;
        }
    }

    public abstract Task ExecuteAsync(string[] args);

    public virtual void DisplayHelp()
    {
        Console.WriteLine($"Action: {this.Name}");
        Console.WriteLine("No additional help available.");
    }

    public bool MatchesActionName(string actionName)
    {
        var match = this.Name.Equals(actionName, StringComparison.InvariantCultureIgnoreCase)
            || this.Name.Equals(actionName.Replace("-", ""), StringComparison.InvariantCultureIgnoreCase);

        return match;
    }
}