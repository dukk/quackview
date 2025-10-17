namespace TypoDukk.QuackView.QuackJob.Services;

internal interface ISecretStore
{
    string GetSecret(string key);

    void SetSecret(string key, string value);

    string ExpandSecrets(string input);
}

internal class SecretStore : ISecretStore
{
    private readonly Dictionary<string, string> secrets = new(StringComparer.OrdinalIgnoreCase);

    public string GetSecret(string key)
    {
        if (secrets.TryGetValue(key, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"Secret with key '{key}' not found.");
    }

    public void SetSecret(string key, string value)
    {
        secrets[key] = value;
    }

    public string ExpandSecrets(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        foreach (var kvp in secrets)
            input = input.Replace($"${{{kvp.Key}}}", kvp.Value, StringComparison.OrdinalIgnoreCase);

        if (input.Contains("${"))
            throw new KeyNotFoundException("One or more secrets in the input string were not found in the secret store.");

        return input;
    }
}