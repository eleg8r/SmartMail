using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Domain.Entities;

namespace SmartMail.Persistence.Repositories;

public class BatchScheduleRepository : IBatchScheduleRepository
{
    private readonly string _connectionString;

    public BatchScheduleRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<BatchSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var data = await connection.QueryFirstOrDefaultAsync(
            "SELECT * FROM BatchSchedules WHERE Id = @Id",
            new { Id = id });

        return data != null ? MapToBatchSchedule(data) : null;
    }

    public async Task<List<BatchSchedule>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var batches = await connection.QueryAsync(
            "SELECT * FROM BatchSchedules WHERE CampaignId = @CampaignId ORDER BY BatchNumber",
            new { CampaignId = campaignId });

        return batches.Select(MapToBatchSchedule).ToList();
    }

    public async Task<Guid> AddAsync(BatchSchedule schedule, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", schedule.Id, DbType.Guid, ParameterDirection.InputOutput);
        parameters.Add("@CampaignId", schedule.CampaignId);
        parameters.Add("@BatchNumber", schedule.BatchNumber);
        parameters.Add("@ScheduledTime", schedule.ScheduledTime);
        parameters.Add("@MaxRecipients", schedule.MaxRecipients);

        await connection.ExecuteAsync(
            "sp_BatchSchedule_Add",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<Guid>("@Id");
    }

    public async Task UpdateAsync(BatchSchedule schedule, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            "sp_BatchSchedule_Update",
            new
            {
                Id = schedule.Id,
                Completed = schedule.Completed,
                CompletedAt = schedule.CompletedAt,
                RecipientsSent = schedule.RecipientsSent
            },
            commandType: CommandType.StoredProcedure);
    }

    private BatchSchedule MapToBatchSchedule(dynamic data)
    {
        var schedule = BatchSchedule.Create(
            (Guid)data.CampaignId,
            (int)data.BatchNumber,
            (DateTime)data.ScheduledTime,
            (int)data.MaxRecipients);

        // Set Id using reflection
        var idProperty = typeof(BatchSchedule).BaseType?.GetProperty("Id");
        idProperty?.SetValue(schedule, (Guid)data.Id);

        // Set completion status
        if ((bool)data.Completed)
        {
            var recipientsSentProperty = typeof(BatchSchedule).GetProperty("RecipientsSent");
            recipientsSentProperty?.SetValue(schedule, (int)data.RecipientsSent);

            var completedAtProperty = typeof(BatchSchedule).GetProperty("CompletedAt");
            completedAtProperty?.SetValue(schedule, data.CompletedAt);

            var completedProperty = typeof(BatchSchedule).GetProperty("Completed");
            completedProperty?.SetValue(schedule, true);
        }

        return schedule;
    }
}
