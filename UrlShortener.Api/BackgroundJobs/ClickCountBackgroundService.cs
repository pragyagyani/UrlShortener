using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Persistence;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.BackgroundJobs;

public class ClickCountBackgroundService(
    ClickEventChannel clickEventChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<ClickCountBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var shortCode in clickEventChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();

                var rowsUpdated = await dbContext.ShortUrls
                    .Where(shortUrl => shortUrl.ShortCode == shortCode)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            shortUrl => shortUrl.AccessCount,
                            shortUrl => shortUrl.AccessCount + 1),
                        stoppingToken);

                if (rowsUpdated == 0)
                {
                    logger.LogWarning("No short URL found to increment access count for {ShortCode}", shortCode);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to increment access count for {ShortCode}", shortCode);
            }
        }
    }
}
