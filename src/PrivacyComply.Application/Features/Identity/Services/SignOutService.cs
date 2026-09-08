using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Application.Features.Identity.Services;

public sealed class SignOutService
    : ISignOutService
{
    private readonly IAuthenticationSessionCommands
        _authenticationSessionCommands;

    public SignOutService(
        IAuthenticationSessionCommands authenticationSessionCommands)
    {
        _authenticationSessionCommands =
            authenticationSessionCommands;
    }

    public async Task SignOutAsync(
        SignOutRequest request,
        CancellationToken cancellationToken = default)
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

        await _authenticationSessionCommands
            .RevokeAsync(
                request.AuthenticationSessionId,
                "USER_SIGN_OUT",
                DateTime.UtcNow,
                cancellationToken);
    }
}