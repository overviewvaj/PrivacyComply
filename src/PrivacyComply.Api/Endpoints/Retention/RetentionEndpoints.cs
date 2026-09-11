using PrivacyComply.Api.Filters;
using PrivacyComply.Application.Features.Retention.Queries;
using PrivacyComply.Application.Features.Retention.Services;

namespace PrivacyComply.Api.Endpoints.Retention;

public static class RetentionEndpoints
{
    public static IEndpointRouteBuilder MapRetentionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/organisations/{organisationSlug}/retention")
            .RequireAuthorization()
            .AddEndpointFilter<TenantContextRequiredFilter>();

        group.MapGet(
            "/policies",
            async (
                IRetentionPolicyQueries retentionPolicyQueries,
                CancellationToken cancellationToken) =>
            {
                var retentionPolicies =
                    await retentionPolicyQueries.GetAllAsync(
                        cancellationToken);

                return Results.Ok(retentionPolicies);
            });

        group.MapGet(
            "/coverage",
            async (
                IRetentionCoverageQueries retentionCoverageQueries,
                CancellationToken cancellationToken) =>
            {
                var coverage =
                    await retentionCoverageQueries.GetAllAsync(
                        cancellationToken);

                return Results.Ok(coverage);
            });

        group.MapPost(
            "/policies/{retentionPolicyId:guid}/review",
            async (
                Guid retentionPolicyId,
                IRetentionService retentionService,
                CancellationToken cancellationToken) =>
            {
                await retentionService.RecordPolicyReviewAsync(
                    retentionPolicyId,
                    cancellationToken);

                return Results.Ok(new
                {
                    retentionPolicyId,
                    status = "Review recorded"
                });
            });

        return endpoints;
    }
}