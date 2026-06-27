namespace UrlShortener.Api.DTOs;

public class CreateShortUrlRequest
{
    public string LongUrl { get; set; } = null!;

    public string? CustomAlias { get; set; }
}
