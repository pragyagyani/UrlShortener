using System.Text;
using StackExchange.Redis;

namespace UrlShortener.Api.Services;

public class ShortCodeGenerator(IConnectionMultiplexer redis) : IShortCodeGenerator
{
    private const string CounterKey = "urlshortener:counter";
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var counter = await database.StringIncrementAsync(CounterKey);
        return Encode(counter);
    }

    private static string Encode(long value)
    {
        if (value == 0)
        {
            return "0";
        }

        var result = new StringBuilder();
        while (value > 0)
        {
            result.Insert(0, Base62Chars[(int)(value % 62)]);
            value /= 62;
        }

        return result.ToString();
    }
}
