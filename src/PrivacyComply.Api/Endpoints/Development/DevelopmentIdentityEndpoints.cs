using Microsoft.AspNetCore.Mvc;
using PrivacyComply.Application.Abstractions.Identity;

namespace PrivacyComply.Api.Endpoints.Development;

public static class DevelopmentIdentityEndpoints
{
    public static IEndpointRouteBuilder MapDevelopmentIdentityEndpoints(
        this IEndpointRouteBuilder endpoints,
        IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return endpoints;
        }

        var group = endpoints.MapGroup(
            "/api/development/identity");

        group.MapPost(
            "/provision-demo-credential",
            async (
                [FromServices] IUserAccountQueries userAccountQueries,
                [FromServices] ILocalCredentialQueries localCredentialQueries,
                [FromServices] ILocalCredentialCommands localCredentialCommands,
                [FromServices] IPasswordHashService passwordHashService,
                CancellationToken cancellationToken) =>
            {
                const string normalizedEmail =
                    "DEMO.CLIENTADMIN@PRIVACYCOMPLY.LOCAL";

                const string demoPassword =
                    "PrivacyComply-Demo-2026!";

                var user =
                    await userAccountQueries
                        .GetByNormalizedEmailAsync(
                            normalizedEmail,
                            cancellationToken);

                if (user is null)
                {
                    return Results.NotFound(
                        new
                        {
                            message =
                                "Demo client administrator user was not found."
                        });
                }

                var existingCredential =
                    await localCredentialQueries.GetActiveAsync(
                        user.UserAccountId,
                        cancellationToken);

                if (existingCredential is not null)
                {
                    return Results.Conflict(
                        new
                        {
                            message =
                                "An active local credential already exists for the demo user."
                        });
                }

                var passwordHash =
                    passwordHashService.HashPassword(
                        user.UserAccountId,
                        demoPassword);

                await localCredentialCommands.CreateAsync(
                    new CreateLocalCredentialCommand(
                        user.UserAccountId,
                        passwordHash,
                        "ASPNETCORE_IDENTITY_V3",
                        1,
                        false),
                    cancellationToken);

                return Results.Ok(
                    new
                    {
                        message =
                            "Development demo credential provisioned successfully.",
                        emailAddress =
                            user.EmailAddress
                    });
            });

        return endpoints;
    }
}