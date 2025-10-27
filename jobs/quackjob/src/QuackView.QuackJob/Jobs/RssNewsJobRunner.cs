using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Data;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Jobs;

[JobRunner("rss-news", "")]
internal class RssNewsJobRunner(
    ILogger<RssNewsJobRunner> logger,
    IDiskIOService disk,
    IDataFileService dataFile)
    : JobRunner<JobFile<RssNewsJobConfig>>(disk)
{
    protected readonly ILogger<RssNewsJobRunner> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IDataFileService DataFile = dataFile ?? throw new ArgumentNullException(nameof(dataFile));

    public override async Task ExecuteJobFileAsync(JobFile<RssNewsJobConfig> jobFile)
    {
        if (null == jobFile.Config)
            throw new ArgumentException("Invalid job configuration.", nameof(jobFile));

        if (null == jobFile.Config.Feeds || jobFile.Config.Feeds.Count == 0)
            throw new ArgumentException("Invalid job configuration. No RSS feeds configured.", nameof(jobFile));

        if (string.IsNullOrWhiteSpace(jobFile.Config.OutputDataFilePath))
            throw new ArgumentException("Invalid job configuration. No output data file path configured.", nameof(jobFile));

        var allArticles = new List<NewsArticle>();

        foreach (var feed in jobFile.Config.Feeds)
        {
            try
            {
                var feedArticles = await this.FetchRssFeedAsync(feed);
                allArticles.AddRange(feedArticles);
            }
            catch (Exception exception)
            {
                this.Logger.LogError(exception, "Failed to fetch RSS feed {feed}", feed.Url);
            }
        }

        var sortedArticles = allArticles
            .OrderByDescending(a => a.Date)
            .ToList();

        await this.DataFile.WriteJsonFileAsync(jobFile.Config.OutputDataFilePath, sortedArticles);
    }

    public async Task<IList<NewsArticle>> FetchRssFeedAsync(RssFeedConfig feed)
    {
        var articles = new List<NewsArticle>();
        var httpClient = new HttpClient();

        this.Logger.LogDebug("Fetching RSS feed {feed}", feed.Url);

        var response = await httpClient.GetAsync(feed.Url);

        if (!response.IsSuccessStatusCode)
        {
            this.Logger.LogWarning("Failed to fetch RSS feed {feed}, status code: {statusCode}", feed.Url, response.StatusCode);
            return articles.AsReadOnly();
        }

        var content = await response.Content.ReadAsStringAsync();
        var doc = new XmlDocument();
        doc.LoadXml(content);

        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
        namespaceManager.AddNamespace("media", "http://search.yahoo.com/mrss/");
        namespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
        namespaceManager.AddNamespace("content", "http://purl.org/rss/1.0/modules/content/");

        var channelTitle = doc.SelectSingleNode("//*[local-name()='channel']/*[local-name()='title']")?.InnerText
            ?? feed.Url;

        // Get channel image as fallback
        var channelImageUrl = doc.SelectSingleNode("//*[local-name()='channel']/*[local-name()='image']/*[local-name()='url']")?.InnerText
            ?? doc.SelectSingleNode("//*[local-name()='channel']/media:thumbnail/@url", namespaceManager)?.Value
            ?? string.Empty;

        var itemNodes = doc.SelectNodes("//*[local-name()='item']");

        if (itemNodes == null || itemNodes.Count == 0)
        {
            this.Logger.LogWarning("No items found in RSS feed {feed}", feed.Url);
            return articles.AsReadOnly();
        }

        foreach (XmlNode item in itemNodes)
        {
            try
            {
                var title = item.SelectSingleNode("*[local-name()='title']")?.InnerText;
                var description = item.SelectSingleNode("*[local-name()='description']")?.InnerText;
                var pubDateStr = item.SelectSingleNode("*[local-name()='pubDate']")?.InnerText;

                if (string.IsNullOrWhiteSpace(title))
                {
                    this.Logger.LogDebug("Skipping item with no title in feed {feed}", feed.Url);
                    continue;
                }

                var imageUrl = item.SelectSingleNode("media:thumbnail/@url", namespaceManager)?.Value
                    ?? item.SelectSingleNode("*[local-name()='enclosure'][@type='image/jpeg' or @type='image/png']/@url")?.Value
                    ?? channelImageUrl;

                DateTime articleDate = DateTime.MinValue;
                if (DateTime.TryParse(pubDateStr, out var pubDate))
                {
                    articleDate = pubDate.Kind == DateTimeKind.Utc
                        ? pubDate
                        : pubDate.ToUniversalTime();
                }

                var article = new NewsArticle
                {
                    Title = title,
                    Summary = description ?? string.Empty,
                    Date = articleDate,
                    Image = imageUrl,
                    Source = string.IsNullOrWhiteSpace(feed.Source)
                        ? channelTitle
                        : feed.Source
                };
                articles.Add(article);

                if (articles.Count >= feed.MaxItems)
                    break;
            }
            catch (Exception ex)
            {
                this.Logger.LogDebug(ex, "Failed to parse RSS item in feed {feed}", feed.Url);
            }
        }

        this.Logger.LogInformation("Fetched {count} articles from RSS feed {feed} ({source})", articles.Count, feed.Url, channelTitle);
        return articles.AsReadOnly();
    }
}

internal class RssNewsJobConfig : FileOutputJobConfig
{
    public RssNewsJobConfig() : base()
    {
        this.OutputDataFilePath = "news/rss-feeds.json";
    }

    public List<RssFeedConfig> Feeds { get; set; } = [];
}

internal class RssFeedConfig
{
    public string Url { get; set; } = string.Empty;
    public int MaxItems { get; set; } = 5;
    public string Source { get; set; } = string.Empty;
}

/*

World News Feeds:

    BBC World: https://feeds.bbci.co.uk/news/world/rss.xml
        <title>, <description>, <pubDate>, <media:thumbnail url="..."/>

    NBC World News: https://feeds.nbcnews.com/nbcnews/public/news
        <title>, <description>, <pubDate>, <media:thumbnail url="..."/>

    CNBC World News: https://www.cnbc.com/id/100727362/device/rss/rss.html
        <title>, <description>, <pubDate>

    ABC World News: https://abcnews.go.com/abcnews/internationalheadlines
        <title>, <description>, <pubDate>, <media:thumbnail url="..."/> (lots of them)

US News Feeds:

Local News Feeds:


*/