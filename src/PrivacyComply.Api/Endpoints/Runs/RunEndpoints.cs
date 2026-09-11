using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using PrivacyComply.Api.Filters;
using PrivacyComply.Application.Abstractions.Identity;
using PrivacyComply.Application.Abstractions.Tenancy;
using PrivacyComply.Application.Features.Runs.Commands;
using PrivacyComply.Application.Features.Runs.Queries;

namespace PrivacyComply.Api.Endpoints.Runs;

public static class RunEndpoints
{
    private const string DiscoveryRunPermission =
        "ORGANISATION.DISCOVERY.RUN";

    public static IEndpointRouteBuilder MapRunEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/organisations/{organisationSlug}/runs")
            .RequireAuthorization()
            .AddEndpointFilter<TenantContextRequiredFilter>();

        group.MapGet(
            "",
            async (
                IAnalysisRunQueries analysisRunQueries,
                CancellationToken cancellationToken) =>
            {
                var runs =
                    await analysisRunQueries.GetAllAsync(
                        cancellationToken);

                return Results.Ok(runs);
            });

        group.MapPost(
            "",
            async (
                CreateAnalysisRunRequest request,
                HttpContext httpContext,
                ITenantContext tenantContext,
                IUserPermissionQueries userPermissionQueries,
                IAnalysisRunCommands analysisRunCommands,
                IAntiforgery antiforgery,
                CancellationToken cancellationToken) =>
            {
                await antiforgery.ValidateRequestAsync(
                    httpContext);

                if (!tenantContext.HasTenant)
                {
                    return Results.Forbid();
                }

                var userAccountIdValue =
                    httpContext.User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (
                    !Guid.TryParse(
                        userAccountIdValue,
                        out var userAccountId)
                )
                {
                    return Results.Unauthorized();
                }

                var permissions =
                    await userPermissionQueries
                        .GetEffectivePermissionsAsync(
                            userAccountId,
                            tenantContext.OrganisationId,
                            cancellationToken);

                var canRunDiscovery =
                    permissions.Any(
                        permission =>
                            string.Equals(
                                permission.PermissionCode,
                                DiscoveryRunPermission,
                                StringComparison.OrdinalIgnoreCase));

                if (!canRunDiscovery)
                {
                    return Results.Forbid();
                }

                var sourceTypeCode =
                    request.SourceTypeCode
                        .Trim()
                        .ToUpperInvariant();

                if (
                    sourceTypeCode is not "EXCEL"
                    and not "CSV"
                )
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "unsupported_source_type",
                        });
                }

                var sourceName =
                    request.SourceName.Trim();

                if (string.IsNullOrWhiteSpace(sourceName))
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "source_name_required",
                        });
                }

                var correlationId =
                    httpContext.TraceIdentifier;

                var result =
                    await analysisRunCommands.CreateAsync(
                        new CreateAnalysisRunCommand(
                            tenantContext.OrganisationId,
                            sourceTypeCode,
                            sourceName,
                            request.SourceObjectName,
                            userAccountId,
                            correlationId),
                        cancellationToken);

                return Results.Created(
                    $"/api/organisations/{httpContext.Request.RouteValues["organisationSlug"]}/runs",
                    result);
            });

        // NEW
        // Allows an authorised user to cancel a run
        // only while that run is still in QUEUED status.
        group.MapPost(
            "/{analysisRunId:guid}/cancel",
            async (
                Guid analysisRunId,
                HttpContext httpContext,
                ITenantContext tenantContext,
                IUserPermissionQueries userPermissionQueries,
                ICancelAnalysisRunCommand cancelAnalysisRunCommand,
                IAntiforgery antiforgery,
                CancellationToken cancellationToken) =>
            {
                // NEW
                // Cancellation is a state-changing operation,
                // so it must pass antiforgery validation.
                await antiforgery.ValidateRequestAsync(
                    httpContext);

                // NEW
                if (!tenantContext.HasTenant)
                {
                    return Results.Forbid();
                }

                // NEW
                // Read the authenticated user from the
                // trusted server-side claims identity.
                var userAccountIdValue =
                    httpContext.User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                // NEW
                if (
                    !Guid.TryParse(
                        userAccountIdValue,
                        out var userAccountId)
                )
                {
                    return Results.Unauthorized();
                }

                // NEW
                // Resolve effective permissions for the
                // authenticated user within this tenant.
                var permissions =
                    await userPermissionQueries
                        .GetEffectivePermissionsAsync(
                            userAccountId,
                            tenantContext.OrganisationId,
                            cancellationToken);

                // NEW
                // For now, the same permission that allows
                // discovery execution also allows cancelling
                // a queued discovery run.
                var canRunDiscovery =
                    permissions.Any(
                        permission =>
                            string.Equals(
                                permission.PermissionCode,
                                DiscoveryRunPermission,
                                StringComparison.OrdinalIgnoreCase));

                // NEW
                if (!canRunDiscovery)
                {
                    return Results.Forbid();
                }

                try
                {
                    // NEW
                    var result =
                        await cancelAnalysisRunCommand
                            .CancelAsync(
                                new CancelAnalysisRunRequest(
                                    tenantContext.OrganisationId,
                                    analysisRunId,
                                    userAccountId),
                                cancellationToken);

                    // NEW
                    return Results.Ok(result);
                }
                catch (InvalidOperationException)
                {
                    // NEW
                    // This includes cases where:
                    // - the run does not exist,
                    // - the run belongs to another tenant,
                    // - the run has already started,
                    // - the run has already completed,
                    // - the run has already been cancelled.
                    return Results.Conflict(
                        new
                        {
                            error =
                                "analysis_run_not_cancellable",

                            message =
                                "The analysis run could not be cancelled because it does not exist or is no longer queued."
                        });
                }
            });

        return endpoints;
    }
}