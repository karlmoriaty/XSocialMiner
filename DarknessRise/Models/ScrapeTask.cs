using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;

namespace DarknessRise.Models;

public class ScrapeTask
{
    [Key]
    public int Id { get; set; }

    public string Keyword { get; set; } = string.Empty;
    public int MaxTweets { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Log { get; set; }

    public ObservableCollection<Tweet> Tweets { get; set; } = new();
}
