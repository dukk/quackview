using System.Diagnostics.CodeAnalysis;

internal interface IFileService
{
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    bool Exists(string path);
    void DeleteFile(string path);
    void MoveFile(string sourcePath, string destinationPath);
}

[ExcludeFromCodeCoverage]
internal class FileService : IFileService
{
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string content)
    {
        File.WriteAllText(path, content);
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public void MoveFile(string sourcePath, string destinationPath)
    {
        File.Move(sourcePath, destinationPath);
    }
}