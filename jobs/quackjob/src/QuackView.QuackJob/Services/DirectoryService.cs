internal interface IDirectoryService
{
    bool Exists(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern);
}

internal class DirectoryService : IDirectoryService
{
    public bool Exists(string path)
    {
        return Directory.Exists(path);
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
    {
        return Directory.EnumerateFiles(path, searchPattern);
    }
}