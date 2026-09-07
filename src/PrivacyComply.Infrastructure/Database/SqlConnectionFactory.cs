using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PrivacyComply.Application.Abstractions.Tenancy;

namespace PrivacyComply.Infrastructure.Database;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly ITenantContext _tenantContext;

    public SqlConnectionFactory(
        IConfiguration configuration,
        ITenantContext tenantContext)
    {
        _connectionString =
            configuration.GetConnectionString("PrivacyComplyDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'PrivacyComplyDatabase' was not found.");

        _tenantContext = tenantContext;
    }

    public async Task<SqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                EXEC sys.sp_set_session_context
                    @key = N'OrganisationId',
                    @value = @OrganisationId;
                """;

            var organisationIdParameter =
                command.Parameters.Add("@OrganisationId",
                    System.Data.SqlDbType.UniqueIdentifier);

            organisationIdParameter.Value =
                _tenantContext.HasTenant
                    ? _tenantContext.OrganisationId
                    : DBNull.Value;

            await command.ExecuteNonQueryAsync(cancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}