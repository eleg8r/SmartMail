using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Persistence.Repositories;

public class UnsubscribeRepository : IUnsubscribeRepository
{
    private readonly string _connectionString;

    public UnsubscribeRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<Guid> AddAsync(UnsubscribeRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", request.Id, DbType.Guid, ParameterDirection.InputOutput);
        parameters.Add("@TenantId", request.TenantId.Value);
        parameters.Add("@EmailAddress", request.EmailAddress.Address);
        parameters.Add("@CampaignId", request.CampaignId);
        parameters.Add("@Reason", request.Reason);
        parameters.Add("@IpAddress", request.IpAddress);
        parameters.Add("@UserAgent", request.UserAgent);

        await connection.ExecuteAsync(
            "sp_Unsubscribe_Add",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<Guid>("@Id");
    }

    public async Task<bool> IsUnsubscribedAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var result = await connection.QueryFirstOrDefaultAsync<int>(
            "sp_Unsubscribe_IsUnsubscribed",
            new { TenantId = tenantId, EmailAddress = email },
            commandType: CommandType.StoredProcedure);

        return result > 0;
    }

    public async Task<List<UnsubscribeRequest>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var unsubscribes = await connection.QueryAsync(
            "SELECT * FROM UnsubscribeRequests WHERE TenantId = @TenantId ORDER BY UnsubscribedAt DESC",
            new { TenantId = tenantId });

        return unsubscribes.Select(MapToUnsubscribeRequest).ToList();
    }

    private UnsubscribeRequest MapToUnsubscribeRequest(dynamic data)
    {
        var request = UnsubscribeRequest.Create(
            TenantId.Create((Guid)data.TenantId),
            EmailAddress.Create((string)data.EmailAddress),
            (Guid?)data.CampaignId,
            (string?)data.Reason,
            (string?)data.IpAddress,
            (string?)data.UserAgent);

        // Set Id using reflection
        var idProperty = typeof(UnsubscribeRequest).BaseType?.GetProperty("Id");
        idProperty?.SetValue(request, (Guid)data.Id);

        var unsubscribedAtProperty = typeof(UnsubscribeRequest).GetProperty("UnsubscribedAt");
        unsubscribedAtProperty?.SetValue(request, (DateTime)data.UnsubscribedAt);

        return request;
    }
}
