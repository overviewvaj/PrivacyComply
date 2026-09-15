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

                group.MapGet(
            "/{analysisRunId:guid}/fields",
            async (
                Guid analysisRunId,
                ITenantContext tenantContext,
                IAnalysisRunQueries analysisRunQueries,
                CancellationToken cancellationToken) =>
            {
                if (!tenantContext.HasTenant)
                {
                    return Results.Forbid();
                }

                var fields = await analysisRunQueries.GetDiscoveredFieldsAsync(
                    tenantContext.OrganisationId,
                    analysisRunId,
                    cancellationToken);

                return Results.Ok(fields);
            });

        group.MapGet(
            "/{analysisRunId:guid}/findings",
            async (
                Guid analysisRunId,
                ITenantContext tenantContext,
                IAnalysisRunQueries analysisRunQueries,
                CancellationToken cancellationToken) =>
            {
                if (!tenantContext.HasTenant)
                {
                    return Results.Forbid();
                }

                var findings = await analysisRunQueries.GetFindingsAsync(
                    tenantContext.OrganisationId,
                    analysisRunId,
                    cancellationToken);

                return Results.Ok(findings);
            });

        group.MapGet(
            "/{analysisRunId:guid}/evidence",
            async (
                Guid analysisRunId,
                ITenantContext tenantContext,
                IAnalysisRunQueries analysisRunQueries,
                CancellationToken cancellationToken) =>
            {
                if (!tenantContext.HasTenant)
                {
                    return Results.Forbid();
                }

                var evidence = await analysisRunQueries.GetEvidenceAsync(
                    tenantContext.OrganisationId,
                    analysisRunId,
                    cancellationToken);

                return Results.Ok(evidence);
            });

        group.MapGet(
            "/{analysisRunId:guid}/evaluations",
            async (
                Guid analysisRunId,
                ITenantContext tenantContext,
                IAnalysisRunQueries analysisRunQueries,
                CancellationToken cancellationToken) =>
            {
                if (!tenantContext.HasTenant)
                {
                    return Results.Forbid();
                }

                var evaluations = await analysisRunQueries.GetRuleEvaluationsAsync(
                    tenantContext.OrganisationId,
                    analysisRunId,
                    cancellationToken);

                return Results.Ok(evaluations);
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
                    request.TotalPersonalDataFields < 0 ||
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
                    var discoveredFieldCommands = request.DiscoveredFields?
                        .Select(f => new DiscoveredFieldCommand(
                            f.SourceObjectName,
                            f.FieldName,
                            f.OrdinalPosition,
                            f.InferredDataType,
                            f.ClassificationStatus,
                            f.ClassificationCode,
                            f.ClassificationMethod,
                            f.MatchPercentage,
                            f.PrivacyCategory,
                            f.IsPersonalData,
                            f.IsRegulatedIdentifier,
                            f.NonEmptyCount,
                            f.EmptyCount))
                        .ToList();

                    var findingCommands = request.Findings?
                        .Select(f => new FindingCommand(
                            f.FindingCode,
                            f.FindingCategory,
                            f.Severity,
                            f.FieldName,
                            f.RuleReference,
                            f.Message,
                            f.SafeMetadataJson))
                        .ToList();

                    var evidenceCommands = request.Evidence?
                        .Select(e => new EvidenceRecordCommand(
                            e.EvidenceReference,
                            e.EvidenceTypeCode,
                            e.SourceTypeCode,
                            e.SourceReference,
                            e.FieldName,
                            e.ClassificationCode,
                            e.RuleVersion,
                            e.AgentVersion,
                            e.EvidenceHash,
                            e.HashAlgorithmCode,
                            e.MetadataJson))
                        .ToList();

                    await analysisRunCommands.CompleteAsync(
                        new CompleteAnalysisRunCommand(
                            tenantContext.OrganisationId,
                            analysisRunId,
                            userAccountId,
                            request.SourceObjectName,
                            request.TotalRecordsAnalysed,
                            request.TotalFieldsDiscovered,
                            request.TotalPersonalDataFields,
                            request.TotalUnclassifiedFields,
                            request.ClassificationCoveragePercentage,
                            discoveredFieldCommands,
                            findingCommands,
                            evidenceCommands),
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