using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly string _connectionString;

    public TenantRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var result = await connection.QueryFirstOrDefaultAsync(
            "sp_Tenant_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        return MapToTenant(result);
    }

    public async Task<Tenant?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var result = await connection.QueryFirstOrDefaultAsync(
            "sp_Tenant_GetByTenantId",
            new { TenantId = tenantId },
            commandType: CommandType.StoredProcedure);

        return MapToTenant(result);
    }

    public async Task<Tenant?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var result = await connection.QueryFirstOrDefaultAsync(
            "sp_Tenant_GetByApiKey",
            new { ApiKey = apiKey },
            commandType: CommandType.StoredProcedure);

        return MapToTenant(result);
    }

    public async Task<Guid> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", tenant.Id, DbType.Guid, ParameterDirection.InputOutput);
        parameters.Add("@TenantId", tenant.TenantId.Value);
        parameters.Add("@Name", tenant.Name);
        parameters.Add("@ApiKey", tenant.ApiKey);
        parameters.Add("@IsActive", tenant.IsActive);
        parameters.Add("@MaxEmailsPerDay", tenant.MaxEmailsPerDay);
        parameters.Add("@MaxEmailsPerHour", tenant.MaxEmailsPerHour);
        parameters.Add("@MaxAttachmentSizeInMb", tenant.MaxAttachmentSizeInMb);

        await connection.ExecuteAsync(
            "sp_Tenant_Add",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<Guid>("@Id");
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            "sp_Tenant_Update",
            new
            {
                Id = tenant.Id,
                Name = tenant.Name,
                IsActive = tenant.IsActive,
                MaxEmailsPerDay = tenant.MaxEmailsPerDay,
                MaxEmailsPerHour = tenant.MaxEmailsPerHour,
                MaxAttachmentSizeInMb = tenant.MaxAttachmentSizeInMb
            },
            commandType: CommandType.StoredProcedure);
    }

    private Tenant? MapToTenant(dynamic? data)
    {
        if (data == null)
            return null;

        var tenant = Tenant.Create(
            TenantId.Create((Guid)data.TenantId),
            (string)data.Name,
            (string)data.ApiKey,
            (int)data.MaxEmailsPerDay,
            (int)data.MaxEmailsPerHour,
            (int)data.MaxAttachmentSizeInMb);

        if (!(bool)data.IsActive)
            tenant.Deactivate();

        // Set Id using reflection since it's protected
        var idProperty = typeof(Tenant).BaseType?.GetProperty("Id");
        idProperty?.SetValue(tenant, (Guid)data.Id);

        return tenant;
    }
}
