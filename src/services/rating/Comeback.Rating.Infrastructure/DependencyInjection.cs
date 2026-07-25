namespace Comeback.Rating.Infrastructure;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.BuildingBlocks.Infrastructure.Messaging;
using Comeback.Rating.Application.Common.Interfaces;
using Comeback.Rating.Infrastructure.Messaging;
using Comeback.Rating.Infrastructure.Persistence;
using Comeback.Rating.Infrastructure.Persistence.Repositories;
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
        services.AddDbContext<RatingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RatingDbContext>());
        services.AddScoped<IPlayerXpRepository, PlayerXpRepository>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("rating", false));

            x.AddConsumer<PlayerCareerDataUpdatedConsumer>();
            x.AddConsumer<MatchResultSubmittedConsumer>();

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
