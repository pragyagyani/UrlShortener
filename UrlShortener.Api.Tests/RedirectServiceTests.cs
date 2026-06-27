using Moq;
using UrlShortener.Api.Entities;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

public class RedirectServiceTests
{
    [Fact]
    public async Task GetLongUrlAsync_WhenCacheHit_ReturnsCachedUrl()
    {
        await using var context = new UrlShorteningServiceTestContext();
        context.CacheService
            .Setup(cache => cache.GetLongUrlAsync("my-link", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/cached");

        var service = new RedirectService(context.DbContext, context.CacheService.Object);

        var result = await service.GetLongUrlAsync("my-link");

        Assert.Equal("https://example.com/cached", result);
        context.CacheService.Verify(
            cache => cache.SetLongUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLongUrlAsync_WhenCacheMissAndDatabaseHit_ReturnsAndCachesUrl()
    {
        await using var context = new UrlShorteningServiceTestContext();
        context.CacheService
            .Setup(cache => cache.GetLongUrlAsync("my-link", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        context.DbContext.ShortUrls.Add(new ShortUrl
        {
            ShortCode = "my-link",
            LongUrl = "https://example.com/from-db",
            IsCustomAlias = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.DbContext.SaveChangesAsync();

        var service = new RedirectService(context.DbContext, context.CacheService.Object);

        var result = await service.GetLongUrlAsync("my-link");

        Assert.Equal("https://example.com/from-db", result);
        context.CacheService.Verify(
            cache => cache.SetLongUrlAsync("my-link", "https://example.com/from-db", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLongUrlAsync_WhenCacheMissAndDatabaseMiss_ReturnsNull()
    {
        await using var context = new UrlShorteningServiceTestContext();
        context.CacheService
            .Setup(cache => cache.GetLongUrlAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = new RedirectService(context.DbContext, context.CacheService.Object);

        var result = await service.GetLongUrlAsync("missing");

        Assert.Null(result);
        context.CacheService.Verify(
            cache => cache.SetLongUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
