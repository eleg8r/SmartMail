using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartMail.Application.Common.Interfaces;
using SmartMail.Persistence.Repositories;

namespace SmartMail.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure database options
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        // Register repositories
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IEmailCampaignRepository, EmailCampaignRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IEmailClickRepository, EmailClickRepository>();
        services.AddScoped<IUnsubscribeRepository, UnsubscribeRepository>();
        services.AddScoped<IBatchScheduleRepository, BatchScheduleRepository>();
        services.AddScoped<ICampaignRecipientRepository, CampaignRecipientRepository>();

        return services;
    }
}
