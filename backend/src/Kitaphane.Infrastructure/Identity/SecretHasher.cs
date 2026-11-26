using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Kitaphane.Infrastructure.Identity;

/// <summary>
/// Keyed hashing (HMAC-SHA256) for refresh tokens, verification codes and reset tokens.
/// The key is derived from the server secret, so a leaked database alone cannot be used to brute-force short codes.
/// </summary>
public sealed class SecretHasher(IOptions<JwtOptions> options)
{
    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes($"kitaphane-secret-hasher:{options.Value.Secret}"));

    public string Hash(string value) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value)));

    public bool Verify(string value, string? expectedHash) =>
        expectedHash is not null &&
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(value)),
            Encoding.ASCII.GetBytes(expectedHash));

    /// <summary>Six-digit numeric code, uniformly random.</summary>
    public static string NewNumericCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    /// <summary>256-bit URL-safe random token.</summary>
    public static string NewToken() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
