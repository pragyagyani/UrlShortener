using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class RedirectController(
    IRedirectService redirectService,
    IClickEventPublisher clickEventPublisher) : ControllerBase
{
    [HttpGet("{shortCode}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToLongUrl(
        string shortCode,
        CancellationToken cancellationToken)
    {
        var longUrl = await redirectService.GetLongUrlAsync(shortCode, cancellationToken);

        if (longUrl is null)
        {
            return NotFound(new { error = $"Short URL '{shortCode}' was not found." });
        }

        clickEventPublisher.TryPublish(shortCode);
        return Redirect(longUrl);
    }
}
