using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;
using System.Text.Json;

namespace TypoDukk.QuackView.QuackJob.Jobs;

internal class BuildImageFileListJob(ILogger<BuildImageFileListJob> logger, IDataFileService dataFileService,
    IDataDirectoryService dataDirectoryService, IConsoleService console) : JobRunner
{
    protected readonly ILogger<BuildImageFileListJob> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDataFileService DataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    protected readonly IDataDirectoryService DataDirectoryService = dataDirectoryService ?? throw new ArgumentNullException(nameof(dataDirectoryService));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));

    public override async Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        var config = this.LoadJsonConfig<BuildImageFileListJobConfig>(jsonConfig)
            ?? throw new ArgumentException("Invalid job configuration.", nameof(jsonConfig));

        if (string.IsNullOrWhiteSpace(config.DirectoryPath))
            throw new ArgumentException("Invalid job configuration. DirectoryPath is required.", nameof(config.DirectoryPath));

        this.Console.WriteLine($"Executing build file list job using directory '{config.DirectoryPath}', search pattern '{config.SearchPattern}'{(config.IncludeSubdirectories ? " and including subdirectories" : "")}.");

        var fullFileList = new List<string>();
        
        foreach (var searchPattern in config.SearchPattern.Split('|'))
        {
            var files = await this.DataDirectoryService.EnumerateFilesAsync(
                config.DirectoryPath, searchPattern, config.IncludeSubdirectories);

            fullFileList.AddRange(files);
        }
        
        var imageFileList = new ImageFileList
        {
            Metadata = new ImageFileListMetadata
            {
                Created = DateTime.Now,
                LastUpdated = DateTime.Now,
                Sources = new List<string> { config.DirectoryPath },
                SearchPattern = config.SearchPattern
            },
            Images = [.. fullFileList.Select(f => new ImageFile
            {
                Url = f.Replace("\\", "/")
            }).OrderBy(f => f.Url)]
        };

        this.Console.WriteLine($"Writing output file: {config.OutputDataFile}");
        await this.DataFileService.WriteJsonFileAsync(config.OutputDataFile, imageFileList);
    }
}

internal class BuildImageFileListJobConfig
{
    public string DirectoryPath { get; set; } = string.Empty;

    public string SearchPattern { get; set; } = "*.jpg|*.jpeg|*.png|*.gif|*.bmp|*.svg|*.webp|*.tiff";

    public bool IncludeSubdirectories { get; set; } = true;

    public string OutputDataFile { get; set; } = "file-list.json";
}