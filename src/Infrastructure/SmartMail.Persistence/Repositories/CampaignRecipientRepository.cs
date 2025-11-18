using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Persistence.Repositories;

public class CampaignRecipientRepository : ICampaignRecipientRepository
{
    private readonly string _connectionString;

    public CampaignRecipientRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<List<CampaignRecipient>> GetByCampaignIdAsync(Guid campaignId, bool sentOnly, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var recipients = await connection.QueryAsync(
            "sp_CampaignRecipient_GetByCampaignId",
            new { CampaignId = campaignId, SentOnly = sentOnly },
            commandType: CommandType.StoredProcedure);

        return recipients.Select(MapToCampaignRecipient).ToList();
    }

    public async Task<Guid> AddAsync(CampaignRecipient recipient, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", recipient.Id, DbType.Guid, ParameterDirection.InputOutput);
        parameters.Add("@CampaignId", recipient.CampaignId);
        parameters.Add("@EmailAddress", recipient.EmailAddress.Address);
        parameters.Add("@EmailDisplayName", recipient.EmailAddress.DisplayName);
        parameters.Add("@PersonalizationData", recipient.PersonalizationDataJson);

        await connection.ExecuteAsync(
            "sp_CampaignRecipient_Add",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<Guid>("@Id");
    }

    public async Task UpdateAsync(CampaignRecipient recipient, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            "sp_CampaignRecipient_Update",
            new
            {
                Id = recipient.Id,
                EmailId = recipient.EmailId,
                Sent = recipient.Sent,
                SentAt = recipient.SentAt,
                VariantId = recipient.VariantId,
                DripStepIndex = recipient.DripStepIndex,
                NextScheduledDate = recipient.NextScheduledDate
            },
            commandType: CommandType.StoredProcedure);
    }

    private CampaignRecipient MapToCampaignRecipient(dynamic data)
    {
        var emailAddress = EmailAddress.Create(
            (string)data.EmailAddress,
            (string?)data.EmailDisplayName);

        var recipient = CampaignRecipient.Create(
            (Guid)data.CampaignId,
            emailAddress,
            null); // PersonalizationData would need to be deserialized from JSON

        // Set Id using reflection
        var idProperty = typeof(CampaignRecipient).BaseType?.GetProperty("Id");
        idProperty?.SetValue(recipient, (Guid)data.Id);

        // Set sent status
        if ((bool)data.Sent)
        {
            var emailIdProperty = typeof(CampaignRecipient).GetProperty("EmailId");
            emailIdProperty?.SetValue(recipient, data.EmailId);

            var sentProperty = typeof(CampaignRecipient).GetProperty("Sent");
            sentProperty?.SetValue(recipient, true);

            var sentAtProperty = typeof(CampaignRecipient).GetProperty("SentAt");
            sentAtProperty?.SetValue(recipient, data.SentAt);
        }

        // Set variant if applicable
        if (data.VariantId != null)
        {
            var variantIdProperty = typeof(CampaignRecipient).GetProperty("VariantId");
            variantIdProperty?.SetValue(recipient, data.VariantId);
        }

        // Set drip campaign properties
        if (data.DripStepIndex != null)
        {
            var dripStepProperty = typeof(CampaignRecipient).GetProperty("DripStepIndex");
            dripStepProperty?.SetValue(recipient, data.DripStepIndex);

            var nextScheduledProperty = typeof(CampaignRecipient).GetProperty("NextScheduledDate");
            nextScheduledProperty?.SetValue(recipient, data.NextScheduledDate);
        }

        return recipient;
    }
}
