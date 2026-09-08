namespace PrivacyComply.Api.Authentication;

public sealed class AuthenticationCookieOptions
{
    public const string SectionName = "Authentication:Cookie";

    public string CookieName { get; init; } =
        "__Host-PrivacyComply-Session";

    public bool Secure { get; init; } = true;

    public bool HttpOnly { get; init; } = true;

    public string SameSite { get; init; } = "Strict";
}