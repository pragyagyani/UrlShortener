using Microsoft.AspNetCore.Mvc;
using Moq;
using UrlShortener.Api.Controllers;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Tests;

public class RedirectControllerTests
{
    private readonly Mock<IRedirectService> _redirectService = new();
    private readonly Mock<IClickEventPublisher> _clickEventPublisher = new();

    [Fact]
    public async Task RedirectToLongUrl_WhenShortCodeExists_ReturnsRedirectAndPublishesClick()
    {
        _redirectService
            .Setup(service => service.GetLongUrlAsync("my-link", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/some/long/path");

        var controller = new RedirectController(_redirectService.Object, _clickEventPublisher.Object);

        var result = await controller.RedirectToLongUrl("my-link", CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com/some/long/path", redirectResult.Url);
        Assert.False(redirectResult.Permanent);
        _clickEventPublisher.Verify(publisher => publisher.TryPublish("my-link"), Times.Once);
    }

    [Fact]
    public async Task RedirectToLongUrl_WhenShortCodeDoesNotExist_Returns404AndDoesNotPublishClick()
    {
        _redirectService
            .Setup(service => service.GetLongUrlAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var controller = new RedirectController(_redirectService.Object, _clickEventPublisher.Object);

        var result = await controller.RedirectToLongUrl("missing", CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        _clickEventPublisher.Verify(publisher => publisher.TryPublish(It.IsAny<string>()), Times.Never);
    }
}
