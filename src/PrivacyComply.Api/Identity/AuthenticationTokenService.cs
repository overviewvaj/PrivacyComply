using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Api.Identity;

public sealed class AuthenticationTokenService
    : IAuthenticationTokenService
{
    private readonly JwtAuthenticationOptions _options;

    public AuthenticationTokenService(
        IOptions<JwtAuthenticationOptions> options)
    {
        _options = options.Value;
    }

    public AuthenticationTokenResult CreateToken(
        AuthenticationTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(request));
        }

        if (request.AuthenticationSessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Authentication session ID cannot be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SessionReference))
        {
            throw new ArgumentException(
                "Session reference cannot be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.EmailAddress))
        {
            throw new ArgumentException(
                "Email address cannot be empty.",
                nameof(request));
        }

        if (request.ExpiresDateTime <= request.IssuedDateTime)
        {
            throw new ArgumentException(
                "Token expiry must be later than the issue time.",
                nameof(request));
        }

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _options.SigningKey));

        var signingCredentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                request.UserAccountId.ToString()),

            new(
                ClaimTypes.NameIdentifier,
                request.UserAccountId.ToString()),

            new(
                ClaimTypes.Email,
                request.EmailAddress),

            new(
                ClaimTypes.Name,
                request.DisplayName),

            new(
                "authentication_session_id",
                request.AuthenticationSessionId.ToString()),

            new(
                "session_reference",
                request.SessionReference),

            new(
                "is_platform_user",
                request.IsPlatformUser.ToString().ToLowerInvariant()),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var token =
            new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: request.IssuedDateTime,
                expires: request.ExpiresDateTime,
                signingCredentials: signingCredentials);

        var encodedToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AuthenticationTokenResult(
            encodedToken,
            "Bearer",
            request.ExpiresDateTime);
    }
}