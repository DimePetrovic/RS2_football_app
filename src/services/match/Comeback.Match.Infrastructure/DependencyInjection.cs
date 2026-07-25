namespace Comeback.Match.Infrastructure;

using Comeback.BuildingBlocks.Application.Clients;
using Comeback.BuildingBlocks.Infrastructure.Http;
using Comeback.BuildingBlocks.Infrastructure.Media;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Infrastructure.Http;
using Comeback.Match.Infrastructure.Jobs;
using Comeback.Match.Infrastructure.Messaging;
using Comeback.Match.Infrastructure.Persistence;
using Comeback.Match.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.PostgreSql;
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
        services.AddDbContext<MatchDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IMatchUnitOfWork>(sp => sp.GetRequiredService<MatchDbContext>());
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IMatchReviewRepository, MatchReviewRepository>();
        services.AddScoped<IMatchMediaRepository, MatchMediaRepository>();
        services.AddScoped<IMatchEventPublisher, MassTransitEventPublisher>();
        services.AddCloudinary(configuration);

        services.AddHttpClient<IPlayerRatingService, HttpPlayerRatingService>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["Services:RatingApi"] ?? "http://rating-api:8080");
        });

        services.AddHttpClient<IPlayerGroupClient, HttpPlayerGroupClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["Services:ProfileApi"] ?? "http://profile-api:8080");
        });

        services.AddHttpClient<IPlayerInfoClient, HttpPlayerInfoClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["Services:ProfileApi"] ?? "http://profile-api:8080");
        });

        services.AddMassTransit(x =>
        {
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

        // Background jobs (result reminder + daily sweep) — Hangfire over the match database.
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(
                configuration.GetConnectionString("DefaultConnection"))));
        services.AddHangfireServer();

        services.AddScoped<MatchReminderJob>();
        services.AddScoped<IMatchJobScheduler, HangfireMatchJobScheduler>();

        return services;
    }
}
