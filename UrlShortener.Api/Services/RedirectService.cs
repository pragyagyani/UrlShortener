using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Persistence;

namespace UrlShortener.Api.Services;

public class RedirectService(UrlShortenerDbContext dbContext, ICacheService cacheService) : IRedirectService
{
    public async Task<string?> GetLongUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var cachedLongUrl = await cacheService.GetLongUrlAsync(shortCode, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedLongUrl))
        {
            return cachedLongUrl;
        }

        var shortUrl = await dbContext.ShortUrls
            .AsNoTracking()
            .SingleOrDefaultAsync(url => url.ShortCode == shortCode, cancellationToken);

        if (shortUrl is null)
        {
            return null;
        }

        await cacheService.SetLongUrlAsync(shortUrl.ShortCode, shortUrl.LongUrl, cancellationToken);
        return shortUrl.LongUrl;
    }
}
