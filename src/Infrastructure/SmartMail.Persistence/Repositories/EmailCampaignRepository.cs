using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Persistence.Repositories;

public class EmailCampaignRepository : IEmailCampaignRepository
{
    private readonly string _connectionString;

    public EmailCampaignRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<EmailCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var multi = await connection.QueryMultipleAsync(
            "sp_Campaign_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        var campaignData = await multi.ReadFirstOrDefaultAsync();
        if (campaignData == null)
            return null;

        var recipients = await multi.ReadAsync();
        var batches = await multi.ReadAsync();
        var variants = await multi.ReadAsync();
        var dripSteps = await multi.ReadAsync();

        return MapToCampaign(campaignData, recipients, batches, variants, dripSteps);
    }

    public async Task<List<EmailCampaign>> GetByTenantIdAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var campaigns = await connection.QueryAsync(
            "sp_Campaign_GetByTenantId",
            new { TenantId = tenantId, Skip = skip, Take = take },
            commandType: CommandType.StoredProcedure);

        var result = new List<EmailCampaign>();
        foreach (var campaignData in campaigns)
        {
            // For list queries, we don't load all related data
            result.Add(MapToCampaign(campaignData, null, null, null, null));
        }

        return result;
    }

    public async Task<Guid> AddAsync(EmailCampaign campaign, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", campaign.Id, DbType.Guid, ParameterDirection.InputOutput);
        parameters.Add("@TenantId", campaign.TenantId.Value);
        parameters.Add("@Name", campaign.Name);
        parameters.Add("@Description", campaign.Description);
        parameters.Add("@Type", campaign.Type.ToString());
        parameters.Add("@Status", campaign.Status.ToString());
        parameters.Add("@FromAddress", campaign.FromAddress.Address);
        parameters.Add("@FromDisplayName", campaign.FromAddress.DisplayName);
        parameters.Add("@Subject", campaign.Subject);
        parameters.Add("@HtmlTemplate", campaign.HtmlTemplate);
        parameters.Add("@TextTemplate", campaign.TextTemplate);
        parameters.Add("@ScheduledStartDate", campaign.ScheduledStartDate);
        parameters.Add("@ScheduledEndDate", campaign.ScheduledEndDate);
        parameters.Add("@TimeZone", campaign.TimeZone);
        parameters.Add("@BatchSize", campaign.BatchSize);
        parameters.Add("@MaxEmailsPerHour", campaign.MaxEmailsPerHour);
        parameters.Add("@MaxEmailsPerDay", campaign.MaxEmailsPerDay);
        parameters.Add("@IsAbTest", campaign.IsAbTest);
        parameters.Add("@IsDripCampaign", campaign.IsDripCampaign);
        parameters.Add("@IncludeUnsubscribeLink", campaign.IncludeUnsubscribeLink);
        parameters.Add("@UnsubscribeUrl", campaign.UnsubscribeUrl);
        parameters.Add("@EnableOpenTracking", campaign.EnableOpenTracking);
        parameters.Add("@EnableClickTracking", campaign.EnableClickTracking);

        await connection.ExecuteAsync(
            "sp_Campaign_Add",
            parameters,
            commandType: CommandType.StoredProcedure);

        var campaignId = parameters.Get<Guid>("@Id");

        // Add recipients
        foreach (var recipient in campaign.Recipients)
        {
            var recipientParams = new DynamicParameters();
            recipientParams.Add("@Id", recipient.Id, DbType.Guid, ParameterDirection.InputOutput);
            recipientParams.Add("@CampaignId", campaignId);
            recipientParams.Add("@EmailAddress", recipient.EmailAddress.Address);
            recipientParams.Add("@EmailDisplayName", recipient.EmailAddress.DisplayName);
            recipientParams.Add("@PersonalizationData", recipient.PersonalizationDataJson);

            await connection.ExecuteAsync(
                "sp_CampaignRecipient_Add",
                recipientParams,
                commandType: CommandType.StoredProcedure);
        }

        // Add batch schedules
        foreach (var batch in campaign.BatchSchedules)
        {
            var batchParams = new DynamicParameters();
            batchParams.Add("@Id", batch.Id, DbType.Guid, ParameterDirection.InputOutput);
            batchParams.Add("@CampaignId", campaignId);
            batchParams.Add("@BatchNumber", batch.BatchNumber);
            batchParams.Add("@ScheduledTime", batch.ScheduledTime);
            batchParams.Add("@MaxRecipients", batch.MaxRecipients);

            await connection.ExecuteAsync(
                "sp_BatchSchedule_Add",
                batchParams,
                commandType: CommandType.StoredProcedure);
        }

        return campaignId;
    }

    public async Task UpdateAsync(EmailCampaign campaign, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            "sp_Campaign_Update",
            new
            {
                Id = campaign.Id,
                Status = campaign.Status.ToString(),
                TotalRecipients = campaign.TotalRecipients,
                EmailsSent = campaign.EmailsSent,
                EmailsDelivered = campaign.EmailsDelivered,
                EmailsOpened = campaign.EmailsOpened,
                EmailsClicked = campaign.EmailsClicked,
                EmailsBounced = campaign.EmailsBounced,
                EmailsFailed = campaign.EmailsFailed,
                Unsubscribes = campaign.Unsubscribes,
                SpamComplaints = campaign.SpamComplaints,
                StartedAt = campaign.StartedAt,
                CompletedAt = campaign.CompletedAt,
                PausedAt = campaign.PausedAt,
                CancelledAt = campaign.CancelledAt,
                WinningVariantId = campaign.WinningVariantId
            },
            commandType: CommandType.StoredProcedure);
    }

    private EmailCampaign MapToCampaign(
        dynamic campaignData,
        IEnumerable<dynamic>? recipients,
        IEnumerable<dynamic>? batches,
        IEnumerable<dynamic>? variants,
        IEnumerable<dynamic>? dripSteps)
    {
        var tenantId = TenantId.Create((Guid)campaignData.TenantId);
        var fromAddress = EmailAddress.Create(
            (string)campaignData.FromAddress,
            (string?)campaignData.FromDisplayName);

        var campaignType = Enum.Parse<CampaignType>((string)campaignData.Type);

        var campaign = EmailCampaign.Create(
            tenantId,
            (string)campaignData.Name,
            (string?)campaignData.Description,
            campaignType,
            fromAddress,
            (string)campaignData.Subject,
            (string)campaignData.HtmlTemplate,
            (string?)campaignData.TextTemplate);

        // Set Id using reflection
        var idProperty = typeof(EmailCampaign).BaseType?.GetProperty("Id");
        idProperty?.SetValue(campaign, (Guid)campaignData.Id);

        // Set status
        var statusProperty = typeof(EmailCampaign).GetProperty("Status");
        var status = Enum.Parse<CampaignStatus>((string)campaignData.Status);
        statusProperty?.SetValue(campaign, status);

        // Set statistics
        SetPrivateProperty(campaign, "TotalRecipients", (int)campaignData.TotalRecipients);
        SetPrivateProperty(campaign, "EmailsSent", (int)campaignData.EmailsSent);
        SetPrivateProperty(campaign, "EmailsDelivered", (int)campaignData.EmailsDelivered);
        SetPrivateProperty(campaign, "EmailsOpened", (int)campaignData.EmailsOpened);
        SetPrivateProperty(campaign, "EmailsClicked", (int)campaignData.EmailsClicked);
        SetPrivateProperty(campaign, "EmailsBounced", (int)campaignData.EmailsBounced);
        SetPrivateProperty(campaign, "EmailsFailed", (int)campaignData.EmailsFailed);
        SetPrivateProperty(campaign, "Unsubscribes", (int)campaignData.Unsubscribes);
        SetPrivateProperty(campaign, "SpamComplaints", (int)campaignData.SpamComplaints);

        // Set dates
        SetPrivateProperty(campaign, "StartedAt", campaignData.StartedAt);
        SetPrivateProperty(campaign, "CompletedAt", campaignData.CompletedAt);
        SetPrivateProperty(campaign, "PausedAt", campaignData.PausedAt);
        SetPrivateProperty(campaign, "CancelledAt", campaignData.CancelledAt);

        // Set configuration
        if (campaignData.ScheduledStartDate != null)
        {
            campaign.SetSchedule(
                (DateTime)campaignData.ScheduledStartDate,
                campaignData.ScheduledEndDate,
                (string?)campaignData.TimeZone);
        }

        if (campaignData.MaxEmailsPerHour != null || campaignData.MaxEmailsPerDay != null)
        {
            campaign.SetRateLimits(
                (int?)campaignData.MaxEmailsPerHour,
                (int?)campaignData.MaxEmailsPerDay);
        }

        if (!string.IsNullOrEmpty((string?)campaignData.UnsubscribeUrl))
        {
            campaign.SetUnsubscribeUrl((string)campaignData.UnsubscribeUrl);
        }

        // Load recipients if provided
        if (recipients != null)
        {
            foreach (var recipientData in recipients)
            {
                campaign.AddRecipient(
                    (string)recipientData.EmailAddress,
                    null); // PersonalizationData would need to be deserialized
            }
        }

        // Load batch schedules if provided
        if (batches != null && campaignData.BatchSize != null)
        {
            var batchSchedules = batches.Select(b =>
                BatchSchedule.Create(
                    campaign.Id,
                    (int)b.BatchNumber,
                    (DateTime)b.ScheduledTime,
                    (int)b.MaxRecipients)
            ).ToList();

            campaign.ConfigureBatching((int)campaignData.BatchSize, batchSchedules);
        }

        return campaign;
    }

    private void SetPrivateProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property != null && value != null)
        {
            property.SetValue(obj, value);
        }
    }
}
