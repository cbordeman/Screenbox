using Microsoft.EntityFrameworkCore;

namespace VLC.Net.Database;

public class AppDbContext : DbContext
{
    public DbSet<Setting> Settings { get; set; }

    private readonly string dbPath;

    public AppDbContext()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var settingsDirectory = Path.Combine(userProfile, "VLC.Net");
        Directory.CreateDirectory(settingsDirectory);
        dbPath = Path.Combine(settingsDirectory, "settings.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}