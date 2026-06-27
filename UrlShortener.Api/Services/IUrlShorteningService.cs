using UrlShortener.Api.DTOs;

namespace UrlShortener.Api.Services;

public interface IUrlShorteningService
{
    Task<CreateShortUrlResult> CreateAsync(CreateShortUrlRequest request, CancellationToken cancellationToken = default);
}

public sealed class CreateShortUrlResult
{
    public bool IsSuccess => Error is null;

    public CreateShortUrlResponse? Response { get; init; }

    public CreateShortUrlError? Error { get; init; }

    public string? ErrorMessage { get; init; }

    public static CreateShortUrlResult Success(CreateShortUrlResponse response) =>
        new() { Response = response };

    public static CreateShortUrlResult Failure(CreateShortUrlError error, string message) =>
        new() { Error = error, ErrorMessage = message };
}

public enum CreateShortUrlError
{
    InvalidUrl,
    InvalidAlias,
    AliasConflict
}
