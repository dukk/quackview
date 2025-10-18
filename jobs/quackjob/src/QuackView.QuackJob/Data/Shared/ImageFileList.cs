internal class ImageFileList
{
    public ImageFileListMetadata Metadata { get; set; } = new ImageFileListMetadata();

    public List<ImageFile> Images { get; set; } = new List<ImageFile>();
}

internal class ImageFileListMetadata
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public IList<string> Sources { get; set; } = new List<string>();
    public string? SearchPattern { get; set; } = null;
}

internal class ImageFile
{
    public string Url { get; set; } = string.Empty;
}