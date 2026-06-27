namespace UrlShortener.Api.Entities;

public class ShortUrl
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ShortCode { get; set; } = null!;

    public string LongUrl { get; set; } = null!;

    public bool IsCustomAlias { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public long AccessCount { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}
