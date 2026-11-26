using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Kitaphane.Infrastructure.Identity;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public sealed class TokenService(IOptions<JwtOptions> options, SecretHasher hasher)
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(ApplicationUser user, DateTime now)
    {
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            SigningCredentials = new SigningCredentials(CreateSigningKey(_options.Secret), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    /// <summary>Creates a new refresh token entity and returns it with its raw (unhashed) value.</summary>
    public (string RawToken, RefreshToken Entity) CreateRefreshToken(Guid userId, bool isPersistent, DateTime now)
    {
        var rawToken = SecretHasher.NewToken();

        var entity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = hasher.Hash(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
            IsPersistent = isPersistent,
        };

        return (rawToken, entity);
    }

    public static SymmetricSecurityKey CreateSigningKey(string secret) => new(Encoding.UTF8.GetBytes(secret));
}
