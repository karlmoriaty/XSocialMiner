using System.Collections.ObjectModel;
using System.Text.Json;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using DarknessRise.Data;
using DarknessRise.Models;
using System.IO;

namespace DarknessRise.Services;

public class DatabaseService
{
    public async Task InitializeAsync()
    {
        await using var context = new AppDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task<ObservableCollection<Tweet>> GetAllTweetsAsync()
    {
        await using var context = new AppDbContext();
        var tweets = await context.Tweets.OrderByDescending(t => t.Id).ToListAsync();
        return new ObservableCollection<Tweet>(tweets);
    }

    public async Task AddTweetAsync(Tweet tweet)
    {
        await using var context = new AppDbContext();
        context.Tweets.Add(tweet);
        await context.SaveChangesAsync();
    }

    public async Task AddTweetsAsync(IEnumerable<Tweet> tweets)
    {
        await using var context = new AppDbContext();
        context.Tweets.AddRange(tweets);
        await context.SaveChangesAsync();
    }

    public async Task DeleteTweetAsync(Tweet tweet)
    {
        await using var context = new AppDbContext();
        context.Tweets.Remove(tweet);
        await context.SaveChangesAsync();
    }

    public async Task<ObservableCollection<ScrapeTask>> GetAllTasksAsync()
    {
        await using var context = new AppDbContext();
        var tasks = await context.ScrapeTasks
            .Include(t => t.Tweets)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return new ObservableCollection<ScrapeTask>(tasks);
    }

    public async Task AddTaskAsync(ScrapeTask task)
    {
        await using var context = new AppDbContext();
        context.ScrapeTasks.Add(task);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(ScrapeTask task)
    {
        await using var context = new AppDbContext();
        context.ScrapeTasks.Update(task);
        await context.SaveChangesAsync();
    }

    public async Task ExportToJsonAsync(IEnumerable<Tweet> tweets, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(tweets, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ExportToCsvAsync(IEnumerable<Tweet> tweets, string filePath)
    {
        var csv = new List<string>
        {
            "Id,AuthorName,AuthorHandle,Content,Timestamp,Likes,Retweets,Replies,Bookmarks,Url,ScrapedAt"
        };
        foreach (var tweet in tweets)
        {
            var escapedContent = tweet.Content.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
            csv.Add($"{tweet.Id},\"{tweet.AuthorName}\",\"{tweet.AuthorHandle}\",\"{escapedContent}\",\"{tweet.Timestamp}\",{tweet.Likes},{tweet.Retweets},{tweet.Replies},{tweet.Bookmarks},\"{tweet.Url}\",\"{tweet.ScrapedAt}\"");
        }
        await File.WriteAllLinesAsync(filePath, csv);
    }

    public async Task<List<Tweet>> ImportFromJsonAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var tweets = JsonSerializer.Deserialize<List<Tweet>>(json) ?? new List<Tweet>();
        return tweets;
    }
}
