namespace TypoDukk.QuackView.QuackJob.Data;

internal class JsonListFile<T>
{
    public ListMetadata Metadata { get; set; } = new ListMetadata();

    public IList<T> List { get; set; } = new List<T>();
}

internal class JsonMapFile<T>
{
    public ListMetadata Metadata { get; set; } = new ListMetadata();

    public IDictionary<string, T> Pairs { get; set; } = new Dictionary<string, T>();
}

internal class ListMetadata
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public IList<string> Sources { get; set; } = new List<string>();
}