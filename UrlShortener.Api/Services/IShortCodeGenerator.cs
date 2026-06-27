namespace UrlShortener.Api.Services;

public interface IShortCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
