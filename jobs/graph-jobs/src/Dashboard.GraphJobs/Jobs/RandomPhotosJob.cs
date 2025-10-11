using TypoDukk.Dashboard.GraphJobs.Services;

namespace TypoDukk.Dashboard.GraphJobs.Jobs;

internal class RandomPhotosJob(IPhotosService photosService, IDataFileService dataFileService) 
    : Job<RandomPhotosJobConfig>
{
    private readonly IPhotosService photosService = photosService ?? throw new ArgumentNullException(nameof(photosService));
    private readonly IDataFileService dataFileService = dataFileService ?? throw new ArgumentNullException(nameof(dataFileService));

    public override async Task ExecuteAsync(RandomPhotosJobConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        throw new NotImplementedException();

        // var allPhotos = new List<DriveItem>();

        // foreach (var accountPath in config.AccountPaths)
        // {
        //     var photos = await this.photosService.GetPhotosItemsAsync(accountPath.Path, accountPath.AccountUserName);
        //     allPhotos.AddRange(photos);
        // }

        // var random = new Random();
        // var selectedPhotos = allPhotos
        //     .OrderBy(x => random.Next())
        //     .Take(config.MaxPhotos)
        //     .ToList();

        // await this.dataFileService.WriteJsonFile("photos.json", selectedPhotos);
    }
}

internal class RandomPhotosJobConfig()
{
    public IList<RandomPhotosJobAccountPath> AccountPaths { get; set; } = new List<RandomPhotosJobAccountPath>();

    public int MaxPhotos { get; set; } = 10;
}

internal class RandomPhotosJobAccountPath()
{
    public string AccountUserName { get; set; } = string.Empty;
    public string Path { get; set; } = "/Photos";
}