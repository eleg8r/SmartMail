using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;
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

        // Create tenant using the public Create method (only takes name and description)
        var tenant = Tenant.Create((string)data.Name, (string?)data.Description);

        // Now use reflection to set all the properties from the database
        var tenantType = typeof(Tenant);
        var baseType = tenantType.BaseType; // AggregateRoot<Guid>

        // Set Id (from base class Entity<Guid>)
        SetProperty(baseType, tenant, "Id", (Guid)data.Id);
        SetProperty(baseType, tenant, "CreatedAt", (DateTime)data.CreatedAt);
        SetProperty(baseType, tenant, "UpdatedAt", data.UpdatedAt);

        // Set TenantId
        SetProperty(tenantType, tenant, "TenantId", TenantId.Create((Guid)data.TenantId));

        // Set IsActive status
        SetProperty(tenantType, tenant, "IsActive", (bool)data.IsActive);

        // Set configuration properties
        SetProperty(tenantType, tenant, "DefaultEmailProvider",
            Enum.Parse<EmailProviderType>((string)data.DefaultEmailProvider));
        SetProperty(tenantType, tenant, "MaxAttachmentSizeInMb", (int)data.MaxAttachmentSizeInMb);
        SetProperty(tenantType, tenant, "MaxEmailsPerHour", (int)data.MaxEmailsPerHour);
        SetProperty(tenantType, tenant, "MaxEmailsPerDay", (int)data.MaxEmailsPerDay);
        SetProperty(tenantType, tenant, "EnableOpenTracking", (bool)data.EnableOpenTracking);
        SetProperty(tenantType, tenant, "EnableClickTracking", (bool)data.EnableClickTracking);

        // Set API key properties
        SetProperty(tenantType, tenant, "ApiKey", (string)data.ApiKey);
        SetProperty(tenantType, tenant, "ApiKeyCreatedAt", (DateTime)data.ApiKeyCreatedAt);
        SetProperty(tenantType, tenant, "ApiKeyExpiresAt", data.ApiKeyExpiresAt);

        // Set contact and subscription properties
        SetProperty(tenantType, tenant, "ContactEmail", data.ContactEmail);
        SetProperty(tenantType, tenant, "ContactName", data.ContactName);
        SetProperty(tenantType, tenant, "SubscriptionStartDate", data.SubscriptionStartDate);
        SetProperty(tenantType, tenant, "SubscriptionEndDate", data.SubscriptionEndDate);

        return tenant;
    }

    private void SetProperty(Type? type, object obj, string propertyName, object? value)
    {
        if (type == null) return;

        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
    }
}
