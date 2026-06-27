using Microsoft.AspNetCore.Mvc;
using Moq;
using UrlShortener.Api.Controllers;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

public class UrlsControllerTests
{
    private readonly Mock<IUrlShorteningService> _urlShorteningService = new();
    private readonly Mock<IMetadataService> _metadataService = new();
    private readonly UrlsController _controller;

    public UrlsControllerTests()
    {
        _controller = new UrlsController(_urlShorteningService.Object, _metadataService.Object);
    }

    [Fact]
    public async Task Create_WithCustomAlias_Returns201Created()
    {
        var response = new CreateShortUrlResponse
        {
            ShortCode = "my-link",
            ShortUrl = "http://localhost:5279/my-link",
            LongUrl = "https://example.com/some/long/path",
            CreatedAtUtc = DateTime.UtcNow
        };

        _urlShorteningService
            .Setup(service => service.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShortUrlResult.Success(response));

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/some/long/path",
            CustomAlias = "my-link"
        };

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(response.ShortUrl, createdResult.Location);
        Assert.Equal(response, createdResult.Value);
    }

    [Fact]
    public async Task Create_WithoutCustomAlias_Returns201Created()
    {
        var response = new CreateShortUrlResponse
        {
            ShortCode = "auto-code",
            ShortUrl = "http://localhost:5279/auto-code",
            LongUrl = "https://example.com/another-page",
            CreatedAtUtc = DateTime.UtcNow
        };

        _urlShorteningService
            .Setup(service => service.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShortUrlResult.Success(response));

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/another-page"
        };

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(response, createdResult.Value);
    }

    [Fact]
    public async Task Create_WithDuplicateCustomAlias_Returns409Conflict()
    {
        _urlShorteningService
            .Setup(service => service.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShortUrlResult.Failure(
                CreateShortUrlError.AliasConflict,
                "The alias 'my-link' is already in use."));

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://another-site.com/page",
            CustomAlias = "my-link"
        };

        var result = await _controller.Create(request, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidUrl_Returns400BadRequest()
    {
        _urlShorteningService
            .Setup(service => service.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShortUrlResult.Failure(
                CreateShortUrlError.InvalidUrl,
                "A valid absolute http or https URL is required."));

        var request = new CreateShortUrlRequest
        {
            LongUrl = "not-a-valid-url",
            CustomAlias = "bad-url-test"
        };

        var result = await _controller.Create(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidCustomAlias_Returns400BadRequest()
    {
        _urlShorteningService
            .Setup(service => service.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShortUrlResult.Failure(
                CreateShortUrlError.InvalidAlias,
                "Custom alias must be 3-32 characters and contain only letters, numbers, hyphens, or underscores."));

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/test",
            CustomAlias = "ab"
        };

        var result = await _controller.Create(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Create_WithReservedAlias_Returns400BadRequest()
    {
        _urlShorteningService
            .Setup(service => service.CreateAsync(It.IsAny<CreateShortUrlRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateShortUrlResult.Failure(
                CreateShortUrlError.InvalidAlias,
                "The alias 'api' is reserved."));

        var request = new CreateShortUrlRequest
        {
            LongUrl = "https://example.com/test",
            CustomAlias = "api"
        };

        var result = await _controller.Create(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetMetadata_WhenShortCodeExists_Returns200Ok()
    {
        var metadata = new ShortUrlMetadataResponse
        {
            ShortCode = "my-link",
            LongUrl = "https://example.com/page",
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 10
        };

        _metadataService
            .Setup(service => service.GetMetadataAsync("my-link", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        var result = await _controller.GetMetadata("my-link", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(metadata, okResult.Value);
    }

    [Fact]
    public async Task GetMetadata_WhenShortCodeDoesNotExist_Returns404()
    {
        _metadataService
            .Setup(service => service.GetMetadataAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShortUrlMetadataResponse?)null);

        var result = await _controller.GetMetadata("missing", CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }
}
