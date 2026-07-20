namespace Comeback.Social.Infrastructure;

using Comeback.Social.Application.Common;
using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Infrastructure.Caching;
using Comeback.Social.Infrastructure.Http;
using Comeback.Social.Infrastructure.Messaging;
using Comeback.Social.Infrastructure.Persistence;
using Comeback.Social.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SocialDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ISocialUnitOfWork>(sp => sp.GetRequiredService<SocialDbContext>());
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IUserFeedRepository, UserFeedRepository>();
        services.AddScoped<PostEnricher>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"] ?? "localhost:6379"));
        services.AddScoped<IFeedCache, RedisFeedCache>();

        services.AddHttpClient<IProfileFollowersClient, HttpProfileFollowersClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:ProfileApi"] ?? "http://profile-api:8080");
        });

        services.AddHttpClient<IProfileAvatarsClient, HttpProfileAvatarsClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:ProfileApi"] ?? "http://profile-api:8080");
        });

        services.AddHttpClient<IMatchDetailsClient, HttpMatchDetailsClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:MatchApi"] ?? "http://match-api:8080");
        });

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("social", false));

            x.AddConsumer<MatchResultSubmittedConsumer>();
            x.AddConsumer<PlayerWantedConsumer>();

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
