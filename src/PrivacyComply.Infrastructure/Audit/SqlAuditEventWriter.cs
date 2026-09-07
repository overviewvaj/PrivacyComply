using System.Data;
using Microsoft.Data.SqlClient;
using PrivacyComply.Application.Abstractions.Audit;
using PrivacyComply.Infrastructure.Database;
using PrivacyComply.Application.Abstractions.Observability;

namespace PrivacyComply.Infrastructure.Audit;

public sealed class SqlAuditEventWriter : IAuditEventWriter
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ICorrelationContext _correlationContext;

    public SqlAuditEventWriter(
        SqlConnectionFactory connectionFactory,
        ICorrelationContext correlationContext)
    {
        _connectionFactory = connectionFactory;
        _correlationContext = correlationContext;
    }

    public async Task WriteAsync(
        AuditEventWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = "audit.usp_CreateAuditEvent";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@OrganisationId",
                SqlDbType.UniqueIdentifier)
            {
                Value = request.OrganisationId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EventTypeCode",
                SqlDbType.NVarChar,
                200)
            {
                Value = request.EventTypeCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EventCategoryCode",
                SqlDbType.NVarChar,
                100)
            {
                Value = request.EventCategoryCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ActionCode",
                SqlDbType.NVarChar,
                200)
            {
                Value = request.ActionCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EventStatusCode",
                SqlDbType.NVarChar,
                60)
            {
                Value = request.EventStatusCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ActorTypeCode",
                SqlDbType.NVarChar,
                60)
            {
                Value = request.ActorTypeCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ActorReference",
                SqlDbType.NVarChar,
                300)
            {
                Value = request.ActorReference
                    ?? (object)DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EntityTypeCode",
                SqlDbType.NVarChar,
                200)
            {
                Value = request.EntityTypeCode
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EntityId",
                SqlDbType.UniqueIdentifier)
            {
                Value = request.EntityId
                    ?? (object)DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EntityReference",
                SqlDbType.NVarChar,
                500)
            {
                Value = request.EntityReference
                    ?? (object)DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@CorrelationId",
                SqlDbType.UniqueIdentifier)
            {
                Value = _correlationContext.HasCorrelationId
                    ? _correlationContext.CorrelationId
                    : request.CorrelationId
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EvidenceRecordId",
                SqlDbType.UniqueIdentifier)
            {
                Value = request.EvidenceRecordId
                    ?? (object)DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@EventSummary",
                SqlDbType.NVarChar,
                3000)
            {
                Value = request.EventSummary
            });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}