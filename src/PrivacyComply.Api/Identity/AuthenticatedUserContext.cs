using System.Security.Claims;
using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Api.Identity;

public sealed class AuthenticatedUserContext
    : IAuthenticatedUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticatedUserContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid UserAccountId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userAccountId)
                ? userAccountId
                : Guid.Empty;
        }
    }

    public string? EmailAddress =>
        User?.FindFirstValue(ClaimTypes.Email);

    public string? DisplayName =>
        User?.FindFirstValue(ClaimTypes.Name);

    public bool IsPlatformUser
    {
        get
        {
            var value = User?.FindFirstValue("is_platform_user");

            return bool.TryParse(value, out var isPlatformUser)
                && isPlatformUser;
        }
    }
}