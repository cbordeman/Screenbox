using Avalonia;
using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Splat;
using VLC.Net.Database;

namespace VLC.Net;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Locator.CurrentMutable
            .RegisterLazySingleton<IDbContextFactory<AppDbContext>>(
                () =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                    return new PooledDbContextFactory<AppDbContext>(optionsBuilder.Options);
                });

        var factory = Locator.Current.GetService<IDbContextFactory<AppDbContext>>();
        var dbContext = factory!.CreateDbContext();
        
        if (!dbContext.Database.EnsureCreated())
            dbContext.Database.Migrate();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
