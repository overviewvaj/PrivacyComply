namespace PrivacyComply.Application.Abstractions.Identity;

public interface IPasswordHashService
{
    string HashPassword(
        Guid userAccountId,
        string password);

    bool VerifyPassword(
        Guid userAccountId,
        string passwordHash,
        string providedPassword);
}