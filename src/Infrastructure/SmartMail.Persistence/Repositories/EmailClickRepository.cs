using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;
using SmartMail.Domain.ValueObjects;

namespace SmartMail.Persistence.Repositories;

public class EmailClickRepository : IEmailClickRepository
{
    private readonly string _connectionString;

    public EmailClickRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<Guid> AddAsync(EmailClick emailClick, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", emailClick.Id, DbType.Guid, ParameterDirection.InputOutput);
        parameters.Add("@TenantId", emailClick.TenantId.Value);
        parameters.Add("@EmailId", emailClick.EmailId);
        parameters.Add("@CampaignId", emailClick.CampaignId);
        parameters.Add("@RecipientEmail", emailClick.RecipientEmail.Address);
        parameters.Add("@OriginalUrl", emailClick.OriginalUrl);
        parameters.Add("@TrackedUrl", emailClick.TrackedUrl);
        parameters.Add("@IpAddress", emailClick.IpAddress);
        parameters.Add("@UserAgent", emailClick.UserAgent);
        parameters.Add("@Country", emailClick.Country);
        parameters.Add("@City", emailClick.City);
        parameters.Add("@Device", emailClick.Device);

        await connection.ExecuteAsync(
            "sp_EmailClick_Add",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<Guid>("@Id");
    }

    public async Task<List<EmailClick>> GetByEmailIdAsync(Guid emailId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var clicks = await connection.QueryAsync(
            "SELECT * FROM EmailClicks WHERE EmailId = @EmailId ORDER BY ClickedAt DESC",
            new { EmailId = emailId });

        return clicks.Select(MapToEmailClick).ToList();
    }

    public async Task<List<EmailClick>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var clicks = await connection.QueryAsync(
            "SELECT * FROM EmailClicks WHERE CampaignId = @CampaignId ORDER BY ClickedAt DESC",
            new { CampaignId = campaignId });

        return clicks.Select(MapToEmailClick).ToList();
    }

    private EmailClick MapToEmailClick(dynamic data)
    {
        var tenantId = TenantId.Create((Guid)data.TenantId);
        var recipientEmail = EmailAddress.Create((string)data.RecipientEmail);

        var click = EmailClick.Create(
            tenantId,
            (Guid)data.EmailId,
            (Guid?)data.CampaignId,
            recipientEmail,
            (string)data.OriginalUrl,
            (string)data.TrackedUrl,
            (string?)data.IpAddress,
            (string?)data.UserAgent);

        // Set Id using reflection
        var idProperty = typeof(EmailClick).BaseType?.GetProperty("Id");
        idProperty?.SetValue(click, (Guid)data.Id);

        var clickedAtProperty = typeof(EmailClick).GetProperty("ClickedAt");
        clickedAtProperty?.SetValue(click, (DateTime)data.ClickedAt);

        // Set optional geolocation and device info if available
        if (data.Country != null && data.City != null)
        {
            click.SetGeolocation((string)data.Country, (string)data.City);
        }

        if (data.Device != null)
        {
            click.SetDevice((string)data.Device);
        }

        return click;
    }
}
