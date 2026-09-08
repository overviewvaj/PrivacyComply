namespace PrivacyComply.Application.Abstractions.Identity;

public interface ILocalCredentialCommands
{
    Task CreateAsync(
        CreateLocalCredentialCommand command,
        CancellationToken cancellationToken = default);
}

public sealed record CreateLocalCredentialCommand(
    Guid UserAccountId,
    string PasswordHash,
    string PasswordAlgorithmCode,
    int CredentialVersion,
    bool MustChangePassword);