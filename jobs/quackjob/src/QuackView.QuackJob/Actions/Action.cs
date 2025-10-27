using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal interface IAction
{
    string Name { get; }
    string Description { get; }
    Task ExecuteAsync(string[] args);
    void DisplayHelp();
    bool MatchesActionName(string actionName);
}

internal abstract partial class Action(ILogger<Action> logger, IConsoleService console) : IAction
{
    protected IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));
    protected ILogger<Action> Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [GeneratedRegex("([A-Z])")]
    private static partial Regex nameRegex();

    public virtual string Name => Action.GetNameFromType(this.GetType());

    public virtual string Description { get; protected set; } = "No description available.";

    internal static string GetNameFromType(Type type)
    {
        return Action.GetNameFromType(type.Name);
    }

    internal static string GetNameFromType(string typeName)
    {
        const string ending = "action";
        var name = nameRegex().Replace(typeName, "-$1").ToLower();
        if (name.EndsWith(ending))
            name = name[..^6];
        return name.Trim('-');
    }

    public abstract Task ExecuteAsync(string[] args);

    public virtual void DisplayHelp()
    {
        this.Console.WriteLine($"Action: {this.Name}");
        this.Console.WriteLine("No additional help available.");
    }

    public bool MatchesActionName(string actionName)
    {
        var match = this.Name.Equals(actionName, StringComparison.InvariantCultureIgnoreCase)
            || this.Name.Equals(actionName.Replace("-", ""), StringComparison.InvariantCultureIgnoreCase);

        return match;
    }
}

// Not going to go this far right now... maybe later..
// [AttributeUsage(AttributeTargets.Class, Inherited = false)]
// internal class ActionAttribute(string? name = null, string? description = null, string? shorthand = null) : Attribute
// {
//     public string? Name { get; } = name;
//     public string? Description { get; } = description;
//     public string? Shorthand { get; } = shorthand;
// }

// [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
// internal class ActionFlagAttribute(string? name = null, string? description = null, bool defaultValue = false, string? shorthand = null) : Attribute
// {
//     public string? Name { get; } = name;
//     public string? Description { get; } = description;
//     public bool DefaultValue { get; } = defaultValue;
//     public string? Shorthand { get; } = shorthand;
// }

// [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
// internal class ActionParameterAttribute(
//     string? name = null,
//     string? description = null,
//     string? defaultValue = null,
//     bool isRequired = false,
//     string? shorthand = null) 
//     : Attribute
// {
//     public string? Name { get; } = name;
//     public string? Description { get; } = description;
//     public string? DefaultValue { get; } = defaultValue;
//     public bool IsRequired { get; } = isRequired;
//     public string? Shorthand { get; } = shorthand;
// }