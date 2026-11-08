using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DarknessRise.Models;

public class Tweet
{
    [Key]
    public int Id { get; set; }

    public string AuthorName { get; set; } = string.Empty;
    public string AuthorHandle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public int Likes { get; set; }
    public int Retweets { get; set; }
    public int Replies { get; set; }
    public int Bookmarks { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ScrapedAt { get; set; } = string.Empty;

    public int? ScrapeTaskId { get; set; }
    [ForeignKey(nameof(ScrapeTaskId))]
    public ScrapeTask? ScrapeTask { get; set; }
}
