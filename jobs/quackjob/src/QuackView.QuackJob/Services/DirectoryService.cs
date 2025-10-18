internal interface IDirectoryService
{
    void CreateDirectory(string path);
    bool Exists(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool includeSubdirectories = false);
}

internal class DirectoryService : IDirectoryService
{
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool Exists(string path)
    {
        return Directory.Exists(path);
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool includeSubdirectories = false)
    {
        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(path, searchPattern, searchOption);
    }
}