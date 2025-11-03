using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.Enums;
using SmartMail.Domain.ValueObjects;
using System.Data;

namespace SmartMail.Persistence.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly string _connectionString;

    public EmailRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<Email?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.QueryMultipleAsync(
            "sp_Email_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        var emailData = await result.ReadFirstOrDefaultAsync();
        if (emailData == null)
            return null;

        var attachments = (await result.ReadAsync()).ToList();

        return MapToEmail(emailData, attachments);
    }

    public async Task<Email?> GetByTrackingIdAsync(string trackingId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var emailData = await connection.QueryFirstOrDefaultAsync(
            "sp_Email_GetByTrackingId",
            new { TrackingId = trackingId },
            commandType: CommandType.StoredProcedure);

        if (emailData == null)
            return null;

        return MapToEmail(emailData, new List<dynamic>());
    }

    public async Task<List<Email>> GetByTenantIdAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var emailsData = await connection.QueryAsync(
            "sp_Email_GetByTenantId",
            new { TenantId = tenantId, Skip = skip, Take = take },
            commandType: CommandType.StoredProcedure);

        return emailsData.Select(e => MapToEmail(e, new List<dynamic>())).ToList();
    }

    public async Task<Guid> AddAsync(Email email, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", email.Id);
        parameters.Add("@TenantId", email.TenantId.Value);
        parameters.Add("@FromAddress", email.From.Address);
        parameters.Add("@FromDisplayName", email.From.DisplayName);
        parameters.Add("@ToAddress", email.To.Address);
        parameters.Add("@ToDisplayName", email.To.DisplayName);
        parameters.Add("@CcList", SerializeEmailList(email.Cc));
        parameters.Add("@BccList", SerializeEmailList(email.Bcc));
        parameters.Add("@Subject", email.Content.Subject);
        parameters.Add("@HtmlBody", email.Content.HtmlBody);
        parameters.Add("@TextBody", email.Content.TextBody);
        parameters.Add("@Status", email.Status.ToString());
        parameters.Add("@CampaignId", email.CampaignId);
        parameters.Add("@TrackingId", email.TrackingId);
        parameters.Add("@ScheduledAt", email.ScheduledAt);
        parameters.Add("@Metadata", SerializeMetadata(email.Metadata));

        var result = await connection.QuerySingleAsync<Guid>(
            "sp_Email_Add",
            parameters,
            commandType: CommandType.StoredProcedure);

        // Add attachments
        foreach (var attachment in email.Attachments)
        {
            await AddAttachmentAsync(connection, attachment, email.Id);
        }

        return result;
    }

    public async Task UpdateAsync(Email email, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", email.Id);
        parameters.Add("@Status", email.Status.ToString());
        parameters.Add("@OpenTracked", email.OpenTracked);
        parameters.Add("@OpenedAt", email.OpenedAt);
        parameters.Add("@OpenCount", email.OpenCount);
        parameters.Add("@ClickTracked", email.ClickTracked);
        parameters.Add("@FirstClickedAt", email.FirstClickedAt);
        parameters.Add("@ClickCount", email.ClickCount);
        parameters.Add("@SentAt", email.SentAt);
        parameters.Add("@DeliveredAt", email.DeliveredAt);
        parameters.Add("@BouncedAt", email.BouncedAt);
        parameters.Add("@BounceType", email.BounceType?.ToString());
        parameters.Add("@BounceReason", email.BounceReason);
        parameters.Add("@RetryCount", email.RetryCount);
        parameters.Add("@ErrorMessage", email.ErrorMessage);
        parameters.Add("@ProviderUsed", email.ProviderUsed?.ToString());

        await connection.ExecuteAsync(
            "sp_Email_Update",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    private async Task AddAttachmentAsync(IDbConnection connection, EmailAttachment attachment, Guid emailId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", attachment.Id);
        parameters.Add("@EmailId", emailId);
        parameters.Add("@FileName", attachment.FileName);
        parameters.Add("@ContentType", attachment.ContentType);
        parameters.Add("@SizeInBytes", attachment.SizeInBytes);
        parameters.Add("@Content", attachment.Content);

        await connection.ExecuteAsync(
            "sp_EmailAttachment_Add",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    private Email MapToEmail(dynamic emailData, List<dynamic> attachments)
    {
        // Use reflection to create Email entity
        // In production, you'd use a proper mapper or constructor
        var from = EmailAddress.Create((string)emailData.FromAddress, (string?)emailData.FromDisplayName);
        var to = EmailAddress.Create((string)emailData.ToAddress, (string?)emailData.ToDisplayName);
        var content = EmailContent.Create((string)emailData.Subject, (string)emailData.HtmlBody, (string?)emailData.TextBody);
        var tenantId = TenantId.Create((Guid)emailData.TenantId);

        var email = Email.Create(
            tenantId,
            from,
            to,
            content,
            (Guid?)emailData.CampaignId,
            (DateTime?)emailData.ScheduledAt);

        // Set private fields using reflection (for demonstration)
        // In production, consider using a library like AutoMapper or manual mapping
        typeof(Email).GetProperty("Id")!.SetValue(email, (Guid)emailData.Id);

        return email;
    }

    private string? SerializeEmailList(List<EmailAddress> emails)
    {
        if (!emails.Any())
            return null;

        return System.Text.Json.JsonSerializer.Serialize(
            emails.Select(e => new { Address = e.Address, DisplayName = e.DisplayName }));
    }

    private string? SerializeMetadata(Dictionary<string, string> metadata)
    {
        if (!metadata.Any())
            return null;

        return System.Text.Json.JsonSerializer.Serialize(metadata);
    }
}
