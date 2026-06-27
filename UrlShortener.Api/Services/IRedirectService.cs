namespace UrlShortener.Api.Services;

public interface IRedirectService
{
    Task<string?> GetLongUrlAsync(string shortCode, CancellationToken cancellationToken = default);
}
