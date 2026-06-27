using UrlShortener.Api.Entities;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

public class MetadataServiceTests
{
    [Fact]
    public async Task GetMetadataAsync_WhenShortCodeExists_ReturnsMetadata()
    {
        await using var context = new UrlShorteningServiceTestContext();
        var createdAtUtc = DateTime.UtcNow.AddMinutes(-5);

        context.DbContext.ShortUrls.Add(new ShortUrl
        {
            ShortCode = "my-link",
            LongUrl = "https://example.com/page",
            IsCustomAlias = true,
            CreatedAtUtc = createdAtUtc,
            AccessCount = 7
        });
        await context.DbContext.SaveChangesAsync();

        var service = new MetadataService(context.DbContext);

        var result = await service.GetMetadataAsync("my-link");

        Assert.NotNull(result);
        Assert.Equal("my-link", result.ShortCode);
        Assert.Equal("https://example.com/page", result.LongUrl);
        Assert.Equal(createdAtUtc, result.CreatedAtUtc);
        Assert.Equal(7, result.AccessCount);
    }

    [Fact]
    public async Task GetMetadataAsync_WhenShortCodeDoesNotExist_ReturnsNull()
    {
        await using var context = new UrlShorteningServiceTestContext();
        var service = new MetadataService(context.DbContext);

        var result = await service.GetMetadataAsync("missing");

        Assert.Null(result);
    }
}
