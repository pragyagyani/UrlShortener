using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using UrlShortener.Api.Configuration;
using UrlShortener.Api.DTOs;
using UrlShortener.Api.Entities;
using UrlShortener.Api.Persistence;

namespace UrlShortener.Api.Services;

public partial class UrlShorteningService(
    UrlShortenerDbContext dbContext,
    IShortCodeGenerator shortCodeGenerator,
    ICacheService cacheService,
    IOptions<ShortUrlOptions> shortUrlOptions) : IUrlShorteningService
{
    private static readonly HashSet<string> ReservedAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "api",
        "swagger",
        "health",
        "urls",
        "metadata"
    };

    public async Task<CreateShortUrlResult> CreateAsync(
        CreateShortUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return CreateShortUrlResult.Failure(
                CreateShortUrlError.InvalidUrl,
                "A valid absolute http or https URL is required.");
        }

        var normalizedLongUrl = uri.ToString();
        var isCustomAlias = !string.IsNullOrWhiteSpace(request.CustomAlias);
        string shortCode;

        if (isCustomAlias)
        {
            shortCode = request.CustomAlias!.Trim();

            if (!AliasRegex().IsMatch(shortCode))
            {
                return CreateShortUrlResult.Failure(
                    CreateShortUrlError.InvalidAlias,
                    "Custom alias must be 3-32 characters and contain only letters, numbers, hyphens, or underscores.");
            }

            if (ReservedAliases.Contains(shortCode))
            {
                return CreateShortUrlResult.Failure(
                    CreateShortUrlError.InvalidAlias,
                    $"The alias '{shortCode}' is reserved.");
            }
        }
        else
        {
            shortCode = await shortCodeGenerator.GenerateAsync(cancellationToken);
        }

        var shortUrl = new ShortUrl
        {
            ShortCode = shortCode,
            LongUrl = normalizedLongUrl,
            IsCustomAlias = isCustomAlias,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.ShortUrls.Add(shortUrl);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            if (isCustomAlias)
            {
                return CreateShortUrlResult.Failure(
                    CreateShortUrlError.AliasConflict,
                    $"The alias '{shortCode}' is already in use.");
            }

            shortUrl.ShortCode = await shortCodeGenerator.GenerateAsync(cancellationToken);
            dbContext.Entry(shortUrl).State = EntityState.Detached;

            var retryEntity = new ShortUrl
            {
                ShortCode = shortUrl.ShortCode,
                LongUrl = normalizedLongUrl,
                IsCustomAlias = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.ShortUrls.Add(retryEntity);
            await dbContext.SaveChangesAsync(cancellationToken);
            shortUrl = retryEntity;
        }

        await cacheService.SetLongUrlAsync(shortUrl.ShortCode, shortUrl.LongUrl, cancellationToken);

        var baseUrl = shortUrlOptions.Value.BaseUrl.TrimEnd('/');
        return CreateShortUrlResult.Success(new CreateShortUrlResponse
        {
            ShortCode = shortUrl.ShortCode,
            ShortUrl = $"{baseUrl}/{shortUrl.ShortCode}",
            LongUrl = shortUrl.LongUrl,
            CreatedAtUtc = shortUrl.CreatedAtUtc
        });
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return true;
        }

        var message = exception.InnerException?.Message ?? string.Empty;
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]{3,32}$")]
    private static partial Regex AliasRegex();
}
