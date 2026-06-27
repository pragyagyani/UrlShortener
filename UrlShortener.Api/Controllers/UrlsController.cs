using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/urls")]
public class UrlsController(
    IUrlShorteningService urlShorteningService,
    IMetadataService metadataService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateShortUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateShortUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await urlShorteningService.CreateAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateShortUrlError.AliasConflict => Conflict(new { error = result.ErrorMessage }),
                _ => BadRequest(new { error = result.ErrorMessage })
            };
        }

        return Created(result.Response!.ShortUrl, result.Response);
    }

    [HttpGet("{shortCode}/metadata")]
    [ProducesResponseType(typeof(ShortUrlMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetadata(
        string shortCode,
        CancellationToken cancellationToken)
    {
        var metadata = await metadataService.GetMetadataAsync(shortCode, cancellationToken);

        if (metadata is null)
        {
            return NotFound(new { error = $"Short URL '{shortCode}' was not found." });
        }

        return Ok(metadata);
    }
}
