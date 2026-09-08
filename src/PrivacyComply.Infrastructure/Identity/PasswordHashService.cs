using Microsoft.AspNetCore.Identity;
using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Infrastructure.Identity;

public sealed class PasswordHashService
    : IPasswordHashService
{
    private readonly PasswordHasher<PasswordHashUser> _passwordHasher = new();

    public string HashPassword(
        Guid userAccountId,
        string password)
    {
        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account ID cannot be empty.",
                nameof(userAccountId));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));
        }

        var user = new PasswordHashUser(userAccountId);

        return _passwordHasher.HashPassword(
            user,
            password);
    }

    public bool VerifyPassword(
        Guid userAccountId,
        string passwordHash,
        string providedPassword)
    {
        if (userAccountId == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        var user = new PasswordHashUser(userAccountId);

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            passwordHash,
            providedPassword);

        return result is
            PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private sealed record PasswordHashUser(
        Guid UserAccountId);
}