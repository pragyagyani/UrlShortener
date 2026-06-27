namespace UrlShortener.Api.Services;

public interface IClickEventPublisher
{
    bool TryPublish(string shortCode);
}
