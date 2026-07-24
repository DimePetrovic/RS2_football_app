namespace Comeback.Profile.Infrastructure;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Infrastructure.Media;
using Comeback.BuildingBlocks.Infrastructure.Messaging;
using Comeback.Profile.Application.Common.Interfaces;
using Comeback.Profile.Infrastructure.Messaging;
using Comeback.Profile.Infrastructure.Persistence;
using Comeback.Profile.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProfileDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProfileDbContext>());
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IPlayerGroupRepository, PlayerGroupRepository>();
        services.AddScoped<IPlayerFollowRepository, PlayerFollowRepository>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddCloudinary(configuration);

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<ProfileDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.AddConsumer<UserEmailConfirmedIntegrationEventConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "comeback");
                    h.Password(configuration["RabbitMq:Password"] ?? "comeback_dev");
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
