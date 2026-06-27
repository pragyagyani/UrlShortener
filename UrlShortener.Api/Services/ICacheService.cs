namespace UrlShortener.Api.Services;

public interface ICacheService
{
    Task<string?> GetLongUrlAsync(string shortCode, CancellationToken cancellationToken = default);

    Task SetLongUrlAsync(string shortCode, string longUrl, CancellationToken cancellationToken = default);
}
