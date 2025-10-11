using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TypoDukk.Dashboard.GraphJobs.Services;

internal interface IPhotosService
{
    Task<IList<DriveItem>> GetPhotosItemsAsync(string path, string? accountUserName = null);
}

internal class PhotosService(GraphService graphService) : IPhotosService
{
    private readonly GraphService graphService = graphService ?? throw new ArgumentNullException(nameof(graphService));

    public async Task<IList<DriveItem>> GetPhotosItemsAsync(string path, string? accountUserName = null)
    {
        throw new NotImplementedException();
        // this.graphService.ExecuteInContextAsync(async (client) =>
        // {
        //     var drive = await client.Me.Drive.GetAsync();
        //     var items = await drive..ItemWithPath(path).Children.GetAsync();

        //     var photos = new List<DriveItem>();
        //     foreach (var item in items)
        //     {
        //         if (item.File != null && item.File.MimeType.StartsWith("image/"))
        //         {
        //             photos.Add(item);
        //         }
        //     }
        //     return photos;
        // }, accountUserName);
    }
}