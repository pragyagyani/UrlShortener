namespace UrlShortener.Api.Configuration;

public class ShortUrlOptions
{
    public const string SectionName = "ShortUrl";

    public string BaseUrl { get; set; } = "http://localhost:5279";
}
