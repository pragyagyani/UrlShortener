using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

public class ClickEventChannelTests
{
    [Fact]
    public void TryPublish_WritesShortCodeToChannel()
    {
        var channel = new ClickEventChannel();

        var published = channel.TryPublish("my-link");

        Assert.True(published);
        Assert.True(channel.Reader.TryRead(out var shortCode));
        Assert.Equal("my-link", shortCode);
    }
}
