using Microsoft.EntityFrameworkCore;
using DarknessRise.Models;
using System.IO;

namespace DarknessRise.Data;

public class AppDbContext : DbContext
{
    public DbSet<Tweet> Tweets { get; set; }
    public DbSet<ScrapeTask> ScrapeTasks { get; set; }
    public DbSet<Configuration> Configurations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalSocialMiner",
            "lsm.db"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
