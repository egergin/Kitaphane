using Kitaphane.Application.GoogleBooks;
using Kitaphane.Domain.Books;

namespace Kitaphane.Infrastructure.GoogleBooks;

/// <summary>Maps Google Books volumes to Kitaphane suggestions (docs/api/api-design.md §6).</summary>
internal static class GoogleBooksMapper
{
    public static IReadOnlyList<GoogleBookSearchResultDto> Map(GoogleBooksResponse? response) =>
        (response?.Items ?? [])
            .Select(Map)
            .OfType<GoogleBookSearchResultDto>()
            .ToList();

    public static GoogleBookSearchResultDto? Map(GoogleVolume volume)
    {
        var info = volume.VolumeInfo;
        if (string.IsNullOrWhiteSpace(volume.Id) || string.IsNullOrWhiteSpace(info?.Title))
        {
            return null;
        }

        return new GoogleBookSearchResultDto(
            volume.Id,
            info.Title.Trim(),
            MapAuthors(info.Authors),
            info.PageCount is > 0 ? info.PageCount : null,
            MapIsbn(info.IndustryIdentifiers),
            MapCover(info.ImageLinks));
    }

    /// <summary>Multiple authors are joined with ", ".</summary>
    private static string? MapAuthors(IReadOnlyList<string>? authors)
    {
        var names = (authors ?? []).Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()).ToList();
        return names.Count == 0 ? null : string.Join(", ", names);
    }

    /// <summary>ISBN-13 is preferred over ISBN-10; values are normalized and must have a valid check digit.</summary>
    private static string? MapIsbn(IReadOnlyList<GoogleIndustryIdentifier>? identifiers)
    {
        string? Find(string type)
        {
            var value = Isbn.Normalize(identifiers?.FirstOrDefault(identifier => identifier.Type == type)?.Identifier);
            return Isbn.IsValid(value) ? value : null;
        }

        return Find("ISBN_13") ?? Find("ISBN_10");
    }

    /// <summary>Thumbnail (or small thumbnail) URL, always served over HTTPS.</summary>
    private static string? MapCover(GoogleImageLinks? links)
    {
        var url = links?.Thumbnail ?? links?.SmallThumbnail;
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "https://" + url["http://".Length..] : url;
    }
}
