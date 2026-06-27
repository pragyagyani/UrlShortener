using System.Threading.Channels;

namespace UrlShortener.Api.Services;

public sealed class ClickEventChannel : IClickEventPublisher
{
    private readonly Channel<string> _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });

    public ChannelReader<string> Reader => _channel.Reader;

    public bool TryPublish(string shortCode) => _channel.Writer.TryWrite(shortCode);
}
