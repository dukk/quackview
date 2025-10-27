namespace TypoDukk.QuackView.QuackJob.Data;

internal class NewsArticle
{
    public DateTime? Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}