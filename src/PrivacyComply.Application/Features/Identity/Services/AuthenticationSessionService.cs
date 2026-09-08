using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Application.Features.Identity.Services;

public sealed class AuthenticationSessionService
    : IAuthenticationSessionService
{
    private readonly IAuthenticationSessionQueries
        _authenticationSessionQueries;

    private readonly IUserAccountQueries
        _userAccountQueries;

    private readonly IOrganisationMembershipQueries
        _organisationMembershipQueries;

    private readonly IUserPermissionQueries
        _userPermissionQueries;

    public AuthenticationSessionService(
        IAuthenticationSessionQueries authenticationSessionQueries,
        IUserAccountQueries userAccountQueries,
        IOrganisationMembershipQueries organisationMembershipQueries,
        IUserPermissionQueries userPermissionQueries)
    {
        _authenticationSessionQueries =
            authenticationSessionQueries;

        _userAccountQueries =
            userAccountQueries;

        _organisationMembershipQueries =
            organisationMembershipQueries;

        _userPermissionQueries =
            userPermissionQueries;
    }

    public async Task<AuthenticationSessionResult>
        GetCurrentSessionAsync(
            AuthenticationSessionRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserAccountId == Guid.Empty)
        {
            return Unauthenticated(
                "INVALID_USER",
                "The authenticated user is invalid.");
        }

        if (request.AuthenticationSessionId == Guid.Empty)
        {
            return Unauthenticated(
                "INVALID_SESSION",
                "The authentication session is invalid.");
        }

        if (string.IsNullOrWhiteSpace(
                request.SessionReference))
        {
            return Unauthenticated(
                "INVALID_SESSION",
                "The authentication session is invalid.");
        }

        if (string.IsNullOrWhiteSpace(
                request.EmailAddress))
        {
            return Unauthenticated(
                "INVALID_USER",
                "The authenticated user is invalid.");
        }

        var authenticationSession =
            await _authenticationSessionQueries
                .GetActiveByReferenceAsync(
                    request.SessionReference,
                    cancellationToken);

        if (authenticationSession is null)
        {
            return Unauthenticated(
                "SESSION_NOT_FOUND",
                "The authentication session is not active.");
        }

        if (
            authenticationSession.AuthenticationSessionId
            != request.AuthenticationSessionId)
        {
            return Unauthenticated(
                "SESSION_MISMATCH",
                "The authentication session does not match the authenticated token.");
        }

        if (
            authenticationSession.UserAccountId
            != request.UserAccountId)
        {
            return Unauthenticated(
                "SESSION_USER_MISMATCH",
                "The authentication session does not belong to the authenticated user.");
        }

        if (
            !string.Equals(
                authenticationSession.SessionStatusCode,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase))
        {
            return Unauthenticated(
                "SESSION_INACTIVE",
                "The authentication session is not active.");
        }

        if (
            authenticationSession.RevokedDateTime
            is not null)
        {
            return Unauthenticated(
                "SESSION_REVOKED",
                "The authentication session has been revoked.");
        }

        if (
            authenticationSession.ExpiresDateTime
            <= DateTime.UtcNow)
        {
            return Unauthenticated(
                "SESSION_EXPIRED",
                "The authentication session has expired.");
        }

        var normalizedEmailAddress =
            request.EmailAddress
                .Trim()
                .ToUpperInvariant();

        var user =
            await _userAccountQueries
                .GetByNormalizedEmailAsync(
                    normalizedEmailAddress,
                    cancellationToken);

        if (user is null)
        {
            return Unauthenticated(
                "USER_NOT_FOUND",
                "The authenticated user no longer exists.");
        }

        if (
            user.UserAccountId
            != request.UserAccountId)
        {
            return Unauthenticated(
                "USER_MISMATCH",
                "The authenticated user does not match the session.");
        }

        if (user.IsDeleted)
        {
            return Unauthenticated(
                "USER_DELETED",
                "The authenticated user is no longer available.");
        }

        if (
            !string.Equals(
                user.AccountStatusCode,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase))
        {
            return Unauthenticated(
                "USER_INACTIVE",
                "The authenticated user is not active.");
        }

        var memberships =
            await _organisationMembershipQueries
                .GetActiveMembershipsAsync(
                    user.UserAccountId,
                    cancellationToken);

        var organisations =
            new List<
                AuthenticationSessionOrganisationResult>();

        foreach (var membership in memberships)
        {
            if (
                !string.Equals(
                    membership.MembershipStatusCode,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var permissions =
                await _userPermissionQueries
                    .GetEffectivePermissionsAsync(
                        user.UserAccountId,
                        membership.OrganisationId,
                        cancellationToken);

            var roles =
                permissions
                    .Select(permission =>
                        permission.RoleCode)
                    .Where(roleCode =>
                        !string.IsNullOrWhiteSpace(
                            roleCode))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(roleCode =>
                        roleCode)
                    .ToArray();

            var permissionCodes =
                permissions
                    .Select(permission =>
                        permission.PermissionCode)
                    .Where(permissionCode =>
                        !string.IsNullOrWhiteSpace(
                            permissionCode))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(permissionCode =>
                        permissionCode)
                    .ToArray();

            organisations.Add(
                new AuthenticationSessionOrganisationResult(
                    membership.OrganisationId,
                    membership.OrganisationMembershipId,
                    membership.OrganisationCode,
                    membership.OrganisationName,
                    membership.OrganisationSlug,
                    membership.IsPrimaryOrganisation,
                    roles,
                    permissionCodes));
        }

        return new AuthenticationSessionResult(
            IsAuthenticated: true,
            ResultCode: "SUCCESS",
            Message: null,
            UserAccountId: user.UserAccountId,
            DisplayName: user.DisplayName,
            IsPlatformUser: user.IsPlatformUser,
            Organisations: organisations);
    }

    private static AuthenticationSessionResult
        Unauthenticated(
            string resultCode,
            string message)
    {
        return new AuthenticationSessionResult(
            IsAuthenticated: false,
            ResultCode: resultCode,
            Message: message,
            UserAccountId: null,
            DisplayName: null,
            IsPlatformUser: false,
            Organisations:
                Array.Empty<
                    AuthenticationSessionOrganisationResult>());
    }
}