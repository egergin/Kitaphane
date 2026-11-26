namespace Kitaphane.Infrastructure.GoogleBooks;

// Subset of the Google Books "volumes" response that Kitaphane uses.
// https://developers.google.com/books/docs/v1/reference/volumes

internal sealed record GoogleBooksResponse(IReadOnlyList<GoogleVolume>? Items);

internal sealed record GoogleVolume(string? Id, GoogleVolumeInfo? VolumeInfo);

internal sealed record GoogleVolumeInfo(
    string? Title,
    string? Subtitle,
    IReadOnlyList<string>? Authors,
    int? PageCount,
    IReadOnlyList<GoogleIndustryIdentifier>? IndustryIdentifiers,
    GoogleImageLinks? ImageLinks);

internal sealed record GoogleIndustryIdentifier(string? Type, string? Identifier);

internal sealed record GoogleImageLinks(string? SmallThumbnail, string? Thumbnail);
