using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using UrlShortener.Api.BackgroundJobs;
using UrlShortener.Api.Entities;
using UrlShortener.Api.Persistence;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

public class ClickCountBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_IncrementsAccessCountForPublishedClick()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<UrlShortenerDbContext>(options => options.UseSqlite(connection));
        await using var provider = services.BuildServiceProvider();

        await using (var setupScope = provider.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();
            dbContext.Database.EnsureCreated();
            dbContext.ShortUrls.Add(new ShortUrl
            {
                ShortCode = "my-link",
                LongUrl = "https://example.com",
                IsCustomAlias = true,
                CreatedAtUtc = DateTime.UtcNow,
                AccessCount = 0
            });
            await dbContext.SaveChangesAsync();
        }

        var clickEventChannel = new ClickEventChannel();
        clickEventChannel.TryPublish("my-link");

        var backgroundService = new ClickCountBackgroundService(
            clickEventChannel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ClickCountBackgroundService>.Instance);

        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var executeTask = backgroundService.StartAsync(cancellationSource.Token);

        await WaitForAccessCountAsync(provider, expectedCount: 1, cancellationSource.Token);

        await backgroundService.StopAsync(CancellationToken.None);
        await executeTask;

        await connection.DisposeAsync();
    }

    private static async Task WaitForAccessCountAsync(
        ServiceProvider provider,
        long expectedCount,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();
            var accessCount = await dbContext.ShortUrls
                .Where(shortUrl => shortUrl.ShortCode == "my-link")
                .Select(shortUrl => shortUrl.AccessCount)
                .SingleAsync(cancellationToken);

            if (accessCount == expectedCount)
            {
                return;
            }

            await Task.Delay(50, cancellationToken);
        }

        throw new TimeoutException("Background service did not update access count in time.");
    }
}
