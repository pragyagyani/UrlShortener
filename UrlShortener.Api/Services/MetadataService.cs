using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Persistence;

namespace UrlShortener.Api.Services;

public class MetadataService(UrlShortenerDbContext dbContext) : IMetadataService
{
    public async Task<ShortUrlMetadataResponse?> GetMetadataAsync(
        string shortCode,
        CancellationToken cancellationToken = default)
    {
        var shortUrl = await dbContext.ShortUrls
            .AsNoTracking()
            .SingleOrDefaultAsync(url => url.ShortCode == shortCode, cancellationToken);

        if (shortUrl is null)
        {
            return null;
        }

        return new ShortUrlMetadataResponse
        {
            ShortCode = shortUrl.ShortCode,
            LongUrl = shortUrl.LongUrl,
            CreatedAtUtc = shortUrl.CreatedAtUtc,
            AccessCount = shortUrl.AccessCount
        };
    }
}
