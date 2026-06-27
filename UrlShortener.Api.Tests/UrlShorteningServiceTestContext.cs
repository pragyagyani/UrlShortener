using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using UrlShortener.Api.Configuration;
using UrlShortener.Api.Persistence;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

internal sealed class UrlShorteningServiceTestContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public UrlShortenerDbContext DbContext { get; }

    public Mock<IShortCodeGenerator> ShortCodeGenerator { get; } = new();

    public Mock<ICacheService> CacheService { get; } = new();

    public UrlShorteningService Service { get; }

    public UrlShorteningServiceTestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<UrlShortenerDbContext>()
            .UseSqlite(_connection)
            .Options;

        DbContext = new UrlShortenerDbContext(options);
        DbContext.Database.EnsureCreated();

        ShortCodeGenerator
            .Setup(generator => generator.GenerateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("auto-code");

        var shortUrlOptions = Options.Create(new ShortUrlOptions
        {
            BaseUrl = "http://localhost:5279"
        });

        Service = new UrlShorteningService(
            DbContext,
            ShortCodeGenerator.Object,
            CacheService.Object,
            shortUrlOptions);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
