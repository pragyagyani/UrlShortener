using Moq;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

public class UrlShorteningServiceTests
{
    [Fact]
    public async Task CreateAsync_WithCustomAlias_ReturnsCreatedResponse()
    {
        await using var context = new UrlShorteningServiceTestContext();

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/some/long/path",
            CustomAlias = "my-link"
        };

        var result = await context.Service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Response);
        Assert.Equal("my-link", result.Response.ShortCode);
        Assert.Equal("http://localhost:5279/my-link", result.Response.ShortUrl);
        Assert.Equal("https://example.com/some/long/path", result.Response.LongUrl);
        Assert.True(result.Response.CreatedAtUtc <= DateTime.UtcNow);

        context.CacheService.Verify(
            cache => cache.SetLongUrlAsync("my-link", "https://example.com/some/long/path", It.IsAny<CancellationToken>()),
            Times.Once);

        var savedByCode = context.DbContext.ShortUrls.Single(entity => entity.ShortCode == "my-link");
        Assert.True(savedByCode.IsCustomAlias);
    }

    [Fact]
    public async Task CreateAsync_WithoutCustomAlias_UsesGeneratedCode()
    {
        await using var context = new UrlShorteningServiceTestContext();

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/another-page"
        };

        var result = await context.Service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Response);
        Assert.Equal("auto-code", result.Response.ShortCode);
        Assert.Equal("http://localhost:5279/auto-code", result.Response.ShortUrl);
        Assert.Equal("https://example.com/another-page", result.Response.LongUrl);

        context.ShortCodeGenerator.Verify(
            generator => generator.GenerateAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        context.CacheService.Verify(
            cache => cache.SetLongUrlAsync("auto-code", "https://example.com/another-page", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCustomAlias_ReturnsAliasConflict()
    {
        await using var context = new UrlShorteningServiceTestContext();

        var firstRequest = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/first",
            CustomAlias = "my-link"
        };

        var duplicateRequest = new CreateShortUrlRequest
        {
            LongUrl = "https://another-site.com/page",
            CustomAlias = "my-link"
        };

        var firstResult = await context.Service.CreateAsync(firstRequest);
        var duplicateResult = await context.Service.CreateAsync(duplicateRequest);

        Assert.True(firstResult.IsSuccess);
        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(CreateShortUrlError.AliasConflict, duplicateResult.Error);
        Assert.Equal("The alias 'my-link' is already in use.", duplicateResult.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidUrl_ReturnsInvalidUrlError()
    {
        await using var context = new UrlShorteningServiceTestContext();

        var request = new CreateShortUrlRequest
        {
            LongUrl = "not-a-valid-url",
            CustomAlias = "bad-url-test"
        };

        var result = await context.Service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateShortUrlError.InvalidUrl, result.Error);
        Assert.Equal("A valid absolute http or https URL is required.", result.ErrorMessage);
        Assert.Empty(context.DbContext.ShortUrls);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCustomAlias_ReturnsInvalidAliasError()
    {
        await using var context = new UrlShorteningServiceTestContext();

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/test",
            CustomAlias = "ab"
        };

        var result = await context.Service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateShortUrlError.InvalidAlias, result.Error);
        Assert.Equal(
            "Custom alias must be 3-32 characters and contain only letters, numbers, hyphens, or underscores.",
            result.ErrorMessage);
        Assert.Empty(context.DbContext.ShortUrls);
    }

    [Fact]
    public async Task CreateAsync_WithReservedAlias_ReturnsInvalidAliasError()
    {
        await using var context = new UrlShorteningServiceTestContext();

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/test",
            CustomAlias = "api"
        };

        var result = await context.Service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateShortUrlError.InvalidAlias, result.Error);
        Assert.Equal("The alias 'api' is reserved.", result.ErrorMessage);
        Assert.Empty(context.DbContext.ShortUrls);
    }
}
