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
                            userAccountId,
                            correlationId),
                        cancellationToken);

                return Results.Created(
                    $"/api/organisations/{httpContext.Request.RouteValues["organisationSlug"]}/runs",
                    result);
            });

        group.MapPost(
            "/{analysisRunId:guid}/start",
            async (
                Guid analysisRunId,
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

                try
                {
                    await analysisRunCommands.StartAsync(
                        new StartAnalysisRunCommand(
                            tenantContext.OrganisationId,
                            analysisRunId,
                            userAccountId),
                        cancellationToken);

                    return Results.NoContent();
                }
                catch (InvalidOperationException)
                {
                    return Results.Conflict(
                        new
                        {
                            error =
                                "analysis_run_not_startable",

                            message =
                                "The analysis run could not be started because it does not exist or is no longer queued.",
                        });
                }
            });

        group.MapPost(
            "/{analysisRunId:guid}/complete",
            async (
                Guid analysisRunId,
                CompleteAnalysisRunRequest request,
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

                if (
                    request.TotalRecordsAnalysed < 0 ||
                    request.TotalFieldsDiscovered < 0 ||
                    request.TotalUnclassifiedFields < 0
                )
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "invalid_inspection_summary",

                            message =
                                "Inspection summary counts cannot be negative.",
                        });
                }

                if (
                    request.TotalUnclassifiedFields >
                    request.TotalFieldsDiscovered
                )
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "invalid_inspection_summary",

                            message =
                                "The number of unclassified fields cannot exceed the total number of discovered fields.",
                        });
                }

                if (
                    request.ClassificationCoveragePercentage < 0m ||
                    request.ClassificationCoveragePercentage > 100m
                )
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "invalid_classification_coverage",

                            message =
                                "Classification coverage must be between 0 and 100.",
                        });
                }

                try
                {
                    await analysisRunCommands.CompleteAsync(
                        new CompleteAnalysisRunCommand(
                            tenantContext.OrganisationId,
                            analysisRunId,
                            userAccountId,
                            request.SourceObjectName,
                            request.TotalRecordsAnalysed,
                            request.TotalFieldsDiscovered,
                            request.TotalUnclassifiedFields,
                            request.ClassificationCoveragePercentage),
                        cancellationToken);

                    return Results.NoContent();
                }
                catch (InvalidOperationException)
                {
                    return Results.Conflict(
                        new
                        {
                            error =
                                "analysis_run_not_completable",

                            message =
                                "The analysis run could not be completed because it does not exist or is no longer running.",
                        });
                }
            });

        group.MapPost(
            "/{analysisRunId:guid}/fail",
            async (
                Guid analysisRunId,
                string? failureCode,
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

                var normalizedFailureCode =
                    string.IsNullOrWhiteSpace(failureCode)
                        ? "ANALYSIS_EXECUTION_FAILED"
                        : failureCode
                            .Trim()
                            .ToUpperInvariant();

                if (normalizedFailureCode.Length > 100)
                {
                    return Results.BadRequest(
                        new
                        {
                            error =
                                "invalid_failure_code",

                            message =
                                "Failure code cannot exceed 100 characters.",
                        });
                }

                try
                {
                    await analysisRunCommands.FailAsync(
                        new FailAnalysisRunCommand(
                            tenantContext.OrganisationId,
                            analysisRunId,
                            userAccountId,
                            normalizedFailureCode),
                        cancellationToken);

                    return Results.NoContent();
                }
                catch (InvalidOperationException)
                {
                    return Results.Conflict(
                        new
                        {
                            error =
                                "analysis_run_not_failable",

                            message =
                                "The analysis run could not be marked as failed because it does not exist or is already in a terminal state.",
                        });
                }
            });

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

                try
                {
                    var result =
                        await cancelAnalysisRunCommand
                            .CancelAsync(
                                new CancelAnalysisRunRequest(
                                    tenantContext.OrganisationId,
                                    analysisRunId,
                                    userAccountId),
                                cancellationToken);

                    return Results.Ok(result);
                }
                catch (InvalidOperationException)
                {
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