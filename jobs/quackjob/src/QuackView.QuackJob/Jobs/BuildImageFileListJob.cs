using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;
using System.Text.Json;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class BuildImageFileListJob(ILogger<BuildImageFileListJob> logger, IDataFileService dataFileService,
    IDataDirectoryService dataDirectoryService) : JobRunner
{
    private readonly ILogger<BuildImageFileListJob> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    private readonly IDataDirectoryService dataDirectoryService = dataDirectoryService ?? throw new ArgumentNullException(nameof(dataDirectoryService));

    public override async Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        var config = this.LoadJsonConfig<BuildImageFileListJobConfig>(jsonConfig)
            ?? throw new ArgumentException("Invalid job configuration.", nameof(jsonConfig));

        if (string.IsNullOrWhiteSpace(config.DirectoryPath))
            throw new ArgumentException("Invalid job configuration. DirectoryPath is required.", nameof(config.DirectoryPath));

        logger.LogInformation("Executing build file list job.");

        var files = await this.dataDirectoryService.EnumerateFilesAsync(config.DirectoryPath, config.SearchPattern, config.IncludeSubdirectories);
        var imageFileList = new ImageFileList
        {
            Metadata = new ImageFileListMetadata
            {
                Created = DateTime.Now,
                LastUpdated = DateTime.Now,
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