namespace UrlShortener.Api.DTOs;

public class CreateShortUrlResponse
{
    public string ShortCode { get; set; } = null!;

    public string ShortUrl { get; set; } = null!;

    public string LongUrl { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
}
