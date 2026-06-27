using StackExchange.Redis;

namespace UrlShortener.Api.Services;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    public async Task<string?> GetLongUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var longUrl = await database.StringGetAsync(GetCacheKey(shortCode));
        return longUrl.HasValue ? longUrl.ToString() : null;
    }

    public async Task SetLongUrlAsync(string shortCode, string longUrl, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        await database.StringSetAsync(GetCacheKey(shortCode), longUrl, DefaultTtl);
    }

    private static string GetCacheKey(string shortCode) => $"url:{shortCode}";
}
