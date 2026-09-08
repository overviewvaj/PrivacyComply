using System.Security.Cryptography;
using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Application.Features.Identity.Services;

public sealed class SignInService : ISignInService
{
    private readonly IUserAccountQueries _userAccountQueries;
    private readonly ILocalCredentialQueries _localCredentialQueries;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ILoginPolicyQueries _loginPolicyQueries;
    private readonly IUserAccountAuthenticationCommands _userAccountAuthenticationCommands;
    private readonly IAuthenticationEventCommands _authenticationEventCommands;
    private readonly IOrganisationMembershipQueries _organisationMembershipQueries;
    private readonly IUserPermissionQueries _userPermissionQueries;
    private readonly IAuthenticationSessionCommands _authenticationSessionCommands;
    private readonly IAuthenticationTokenService _authenticationTokenService;
    private readonly IAuthenticationRequestContext _authenticationRequestContext;

    public SignInService(
        IUserAccountQueries userAccountQueries,
        ILocalCredentialQueries localCredentialQueries,
        IPasswordHashService passwordHashService,
        ILoginPolicyQueries loginPolicyQueries,
        IUserAccountAuthenticationCommands userAccountAuthenticationCommands,
        IAuthenticationEventCommands authenticationEventCommands,
        IOrganisationMembershipQueries organisationMembershipQueries,
        IUserPermissionQueries userPermissionQueries,
        IAuthenticationSessionCommands authenticationSessionCommands,
        IAuthenticationTokenService authenticationTokenService,
        IAuthenticationRequestContext authenticationRequestContext)
    {
        _userAccountQueries = userAccountQueries;
        _localCredentialQueries = localCredentialQueries;
        _passwordHashService = passwordHashService;
        _loginPolicyQueries = loginPolicyQueries;
        _userAccountAuthenticationCommands = userAccountAuthenticationCommands;
        _authenticationEventCommands = authenticationEventCommands;
        _organisationMembershipQueries = organisationMembershipQueries;
        _userPermissionQueries = userPermissionQueries;
        _authenticationSessionCommands = authenticationSessionCommands;
        _authenticationTokenService = authenticationTokenService;
        _authenticationRequestContext = authenticationRequestContext;
    }

    public async Task<SignInResult> SignInAsync(
        SignInRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var normalizedEmail =
            request.EmailAddress?
                .Trim()
                .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Failed();
        }

        var now = DateTime.UtcNow;

        var loginPolicy =
            await _loginPolicyQueries.GetActiveAsync(
                cancellationToken);

        if (loginPolicy is null)
        {
            throw new InvalidOperationException(
                "No active login policy is configured.");
        }

        var user =
            await _userAccountQueries.GetByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            await RecordAuthenticationEventAsync(
                null,
                "SIGN_IN",
                "FAILED",
                "LOCAL_PASSWORD",
                normalizedEmail,
                "INVALID_CREDENTIALS",
                now,
                cancellationToken);

            return Failed();
        }

        if (!string.Equals(
                user.AccountStatusCode,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase))
        {
            await RecordAuthenticationEventAsync(
                user.UserAccountId,
                "SIGN_IN",
                "FAILED",
                "LOCAL_PASSWORD",
                normalizedEmail,
                "ACCOUNT_NOT_ACTIVE",
                now,
                cancellationToken);

            return Failed();
        }

        if (loginPolicy.RequireEmailConfirmation &&
            !user.EmailConfirmed)
        {
            await RecordAuthenticationEventAsync(
                user.UserAccountId,
                "SIGN_IN",
                "FAILED",
                "LOCAL_PASSWORD",
                normalizedEmail,
                "EMAIL_NOT_CONFIRMED",
                now,
                cancellationToken);

            return Failed();
        }

        if (user.LockoutEndDateTime.HasValue)
        {
            if (user.LockoutEndDateTime.Value > now)
            {
                await RecordAuthenticationEventAsync(
                    user.UserAccountId,
                    "SIGN_IN",
                    "FAILED",
                    "LOCAL_PASSWORD",
                    normalizedEmail,
                    "ACCOUNT_LOCKED",
                    now,
                    cancellationToken);

                return Failed();
            }

            await _userAccountAuthenticationCommands
                .ClearExpiredLockoutAsync(
                    user.UserAccountId,
                    cancellationToken);
        }

        var credential =
            await _localCredentialQueries.GetActiveAsync(
                user.UserAccountId,
                cancellationToken);

        if (credential is null)
        {
            await RecordAuthenticationEventAsync(
                user.UserAccountId,
                "SIGN_IN",
                "FAILED",
                "LOCAL_PASSWORD",
                normalizedEmail,
                "INVALID_CREDENTIALS",
                now,
                cancellationToken);

            return Failed();
        }

        var passwordValid =
            _passwordHashService.VerifyPassword(
                user.UserAccountId,
                credential.PasswordHash,
                request.Password);

        if (!passwordValid)
        {
            var failedCount =
                user.FailedSignInCount + 1;

            DateTime? lockoutEnd = null;

            if (failedCount >=
                loginPolicy.MaximumFailedSignInAttempts)
            {
                lockoutEnd =
                    now.AddMinutes(
                        loginPolicy.LockoutDurationMinutes);
            }

            await _userAccountAuthenticationCommands
                .RecordFailedSignInAsync(
                    user.UserAccountId,
                    failedCount,
                    now,
                    lockoutEnd,
                    cancellationToken);

            await RecordAuthenticationEventAsync(
                user.UserAccountId,
                "SIGN_IN",
                "FAILED",
                "LOCAL_PASSWORD",
                normalizedEmail,
                lockoutEnd.HasValue
                    ? "ACCOUNT_LOCKED"
                    : "INVALID_CREDENTIALS",
                now,
                cancellationToken);

            return Failed();
        }

        var requiresMfa =
            loginPolicy.RequireMfa ||
            (user.IsPlatformUser &&
             loginPolicy.RequireMfaForPlatformUsers);

        if (requiresMfa)
        {
            await RecordAuthenticationEventAsync(
                user.UserAccountId,
                "SIGN_IN",
                "PENDING",
                "LOCAL_PASSWORD",
                normalizedEmail,
                "MFA_REQUIRED",
                now,
                cancellationToken);

            return new SignInResult(
                false,
                "MFA_REQUIRED",
                "Additional authentication is required.",
                user.UserAccountId,
                user.DisplayName,
                user.IsPlatformUser,
                true,
                credential.MustChangePassword,
                null,
                null,
                null,
                Array.Empty<SignInOrganisationResult>());
        }

        var memberships =
            await _organisationMembershipQueries
                .GetActiveMembershipsAsync(
                    user.UserAccountId,
                    cancellationToken);

        var organisations =
            new List<SignInOrganisationResult>();

        foreach (var membership in memberships)
        {
            var effectivePermissions =
                await _userPermissionQueries
                    .GetEffectivePermissionsAsync(
                        user.UserAccountId,
                        membership.OrganisationId,
                        cancellationToken);

            var roles =
                effectivePermissions
                    .Select(x => x.RoleCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToArray();

            var permissions =
                effectivePermissions
                    .Select(x => x.PermissionCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToArray();

            organisations.Add(
                new SignInOrganisationResult(
                    membership.OrganisationId,
                    membership.OrganisationMembershipId,
                    membership.OrganisationCode,
                    membership.OrganisationName,
                    membership.OrganisationSlug,
                    membership.IsPrimaryOrganisation,
                    roles,
                    permissions));
        }

        var sessionReference =
            Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

        var expiresDateTime =
            now.AddMinutes(
                loginPolicy.SessionLifetimeMinutes);

        var session =
            await _authenticationSessionCommands.CreateAsync(
                new CreateAuthenticationSessionCommand(
                    user.UserAccountId,
                    sessionReference,
                    "LOCAL_PASSWORD",
                    now,
                    expiresDateTime,
                    _authenticationRequestContext.ClientIpAddress,
                    _authenticationRequestContext.ClientUserAgent),
                cancellationToken);

        var token =
            _authenticationTokenService.CreateToken(
                new AuthenticationTokenRequest(
                    user.UserAccountId,
                    session.AuthenticationSessionId,
                    sessionReference,
                    user.EmailAddress,
                    user.DisplayName,
                    user.IsPlatformUser,
                    now,
                    expiresDateTime));

        await _userAccountAuthenticationCommands
            .RecordSuccessfulSignInAsync(
                user.UserAccountId,
                now,
                cancellationToken);

        await RecordAuthenticationEventAsync(
            user.UserAccountId,
            "SIGN_IN",
            "SUCCESS",
            "LOCAL_PASSWORD",
            normalizedEmail,
            null,
            now,
            cancellationToken,
            session.AuthenticationSessionId);

        return new SignInResult(
            true,
            "SUCCESS",
            null,
            user.UserAccountId,
            user.DisplayName,
            user.IsPlatformUser,
            false,
            credential.MustChangePassword,
            token.AccessToken,
            token.TokenType,
            token.ExpiresDateTime,
            organisations);
    }

    private async Task RecordAuthenticationEventAsync(
        Guid? userAccountId,
        string eventTypeCode,
        string eventStatusCode,
        string? authenticationMethodCode,
        string? emailAddressReference,
        string? failureReasonCode,
        DateTime eventDateTime,
        CancellationToken cancellationToken,
        Guid? authenticationSessionId = null)
    {
        await _authenticationEventCommands.RecordAsync(
            new AuthenticationEventRecord(
                userAccountId,
                authenticationSessionId,
                eventTypeCode,
                eventStatusCode,
                authenticationMethodCode,
                emailAddressReference,
                failureReasonCode,
                _authenticationRequestContext.CorrelationId,
                _authenticationRequestContext.ClientIpAddress,
                _authenticationRequestContext.ClientUserAgent,
                eventDateTime),
            cancellationToken);
    }

    private static SignInResult Failed()
    {
        return new SignInResult(
            false,
            "INVALID_CREDENTIALS",
            "The email address or password is incorrect.",
            null,
            null,
            false,
            false,
            false,
            null,
            null,
            null,
            Array.Empty<SignInOrganisationResult>());
    }
}