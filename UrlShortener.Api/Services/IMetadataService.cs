using UrlShortener.Api.DTOs;

namespace UrlShortener.Api.Services;

public interface IMetadataService
{
    Task<ShortUrlMetadataResponse?> GetMetadataAsync(string shortCode, CancellationToken cancellationToken = default);
}
