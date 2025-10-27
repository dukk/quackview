using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Jobs;

[JobRunner("build-image-file-list", "Build a list of image files that can be used to cycle through by the display")]
internal class BuildImageFileListJobRunner(
    ILogger<BuildImageFileListJobRunner> logger,
    IDataFileService dataFileService,
    IDataDirectoryService dataDirectoryService,
    IConsoleService console,
    IDiskIOService file)
    : JobRunner<JobFile<BuildImageFileListJobConfig>>(file)
{
    protected readonly ILogger<BuildImageFileListJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDataFileService DataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));
    protected readonly IDataDirectoryService DataDirectoryService = dataDirectoryService ?? throw new ArgumentNullException(nameof(dataDirectoryService));
    protected readonly IConsoleService Console = console ?? throw new ArgumentNullException(nameof(console));

    public override async Task ExecuteJobFileAsync(JobFile<BuildImageFileListJobConfig> jobFile)
    {
        var config = jobFile.Config
            ?? throw new ArgumentException("Invalid job configuration.", nameof(jobFile));

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
                Url = Uri.EscapeDataString("/data/" + f.Replace("\\", "/"))
            }).OrderBy(f => f.Url)]
        };

        this.Console.WriteLine($"Writing output file: {config.OutputDataFilePath}");
        await this.DataFileService.WriteJsonFileAsync(config.OutputDataFilePath, imageFileList);
    }

    // public override async Task CreateNewJobFileAsync(string filePath)
    // {
    //     var content = JsonSerializer.Serialize(new JobFile<BuildImageFileListJobConfig>()
    //     {
    //         Metadata = new()
    //         {
    //             Name = "Build image file list",
    //             Description = "Build a list of image files that can be used to cycle through by the display",
    //             Runner = "build-image-file-list",
    //             Schedule = "* * * * *"
    //         },
    //         Config = new()
    //         {
    //             DirectoryPath = "photos",
    //             IncludeSubdirectories = false,
    //             SearchPattern = "*.png",
    //             OutputDataFilePath = "photos/list.json"
    //         }
    //     }, options: Program.DefaultJsonSerializerOptions);

    //     await this.Disk.AppendAllTextAsync(filePath, content);
    // }
}

internal class BuildImageFileListJobConfig : FileOutputJobConfig
{
    public BuildImageFileListJobConfig() : base()
    {
        this.OutputDataFilePath = "image-list.json";
    }

    public string DirectoryPath { get; set; } = string.Empty;
    public string SearchPattern { get; set; } = "*.jpg|*.jpeg|*.png|*.gif|*.bmp|*.svg|*.webp|*.tiff";
    public bool IncludeSubdirectories { get; set; } = true;
}