namespace UrlShortener.Api.DTOs;

public class ShortUrlMetadataResponse
{
    public string ShortCode { get; set; } = null!;

    public string LongUrl { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public long AccessCount { get; set; }
}
