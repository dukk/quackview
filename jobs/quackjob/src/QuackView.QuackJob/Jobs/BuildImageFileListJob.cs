using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class BuildImageFileListJob(ILogger<BuildImageFileListJob> logger, IDataFileService dataFileService,
    IDataDirectoryService dataDirectoryService) : JobRunner
{
    private readonly ILogger<BuildImageFileListJob> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    private readonly IDataDirectoryService dataDirectoryService = dataDirectoryService ?? throw new ArgumentNullException(nameof(dataDirectoryService));

    public override async Task ExecuteAsync(string? configFile = null, IDictionary<string, string>? parsedArgs = null)
    {
        var config = await this.LoadJsonConfigAsync<BuildImageFileListJobConfig>(configFile);

        if (string.IsNullOrWhiteSpace(config.DirectoryPath))
            throw new ArgumentException("Invalid job configuration. DirectoryPath is required.", nameof(configFile));

        logger.LogInformation("Executing build file list job.");

        var files = await this.dataDirectoryService.EnumerateFilesAsync(config.DirectoryPath, config.SearchPattern, config.IncludeSubdirectories);
        var imageFileList = new ImageFileList
        {
            Metadata = new ImageFileListMetadata
            {
                Created = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                Sources = new List<string> { config.DirectoryPath },
                SearchPattern = config.SearchPattern
            },
            Images = [.. files.Select(f => new ImageFile
            {
                Url = f.Replace("\\", "/")
            })]
        };

        await this.dataFileService.WriteJsonFileAsync(config.OutputDataFile, imageFileList);
    }
}

internal class BuildImageFileListJobConfig
{
    public string DirectoryPath { get; set; } = string.Empty;

    public string SearchPattern { get; set; } = "*.jpg|*.jpeg|*.png|*.gif|*.bmp|*.svg|*.webp|*.tiff";

    public bool IncludeSubdirectories { get; set; } = true;

    public string OutputDataFile { get; set; } = "file-list.json";
}