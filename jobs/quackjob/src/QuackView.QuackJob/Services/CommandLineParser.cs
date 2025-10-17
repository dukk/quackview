namespace TypoDukk.QuackView.QuackJob.Services;

internal interface ICommandLineParser
{
    IDictionary<string, string> ParseArgs(string[] args);
}

internal class CommandLineParser : ICommandLineParser
{
    public IDictionary<string, string> ParseArgs(string[] args)
    {
        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            var pair = arg.Split('=', 2);

            if (pair.Length == 2)
            {
                parts[pair[0].TrimStart('-', '/')] = pair[1].Trim('\'', '"');
            }
            else
            {
                parts[pair[0].TrimStart('-', '/')] = "true";
            }
        }

        return parts;
    }
}