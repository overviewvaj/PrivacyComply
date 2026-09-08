namespace PrivacyComply.Application.Features.Identity.Services;

public interface ISignOutService
{
    Task SignOutAsync(
        SignOutRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SignOutRequest(
    Guid UserAccountId,
    Guid AuthenticationSessionId);