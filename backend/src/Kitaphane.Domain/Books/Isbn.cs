namespace Kitaphane.Domain.Books;

/// <summary>ISBN-10 / ISBN-13 normalization and checksum validation.</summary>
public static class Isbn
{
    /// <summary>Removes hyphens and spaces and upper-cases a trailing x. Returns null for empty input.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return new string(value.Where(character => character is not ('-' or ' ')).ToArray()).ToUpperInvariant();
    }

    /// <summary>Validates a normalized ISBN-10 or ISBN-13 including its check digit.</summary>
    public static bool IsValid(string? normalized) => normalized?.Length switch
    {
        10 => IsValidIsbn10(normalized),
        13 => IsValidIsbn13(normalized),
        _ => false,
    };

    private static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int digit;
            if (char.IsAsciiDigit(isbn[i]))
            {
                digit = isbn[i] - '0';
            }
            else if (i == 9 && isbn[i] == 'X')
            {
                digit = 10;
            }
            else
            {
                return false;
            }

            sum += digit * (10 - i);
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (!isbn.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0;
    }
}
