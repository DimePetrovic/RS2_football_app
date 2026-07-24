namespace Comeback.Chat.Infrastructure;

using Comeback.BuildingBlocks.Application.Clients;
using Comeback.BuildingBlocks.Infrastructure.Http;
using Comeback.Chat.Application.Common.Interfaces;
using Comeback.Chat.Infrastructure.Encryption;
using Comeback.Chat.Infrastructure.Http;
using Comeback.Chat.Infrastructure.Persistence;
using Comeback.Chat.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IChatUnitOfWork>(sp => sp.GetRequiredService<ChatDbContext>());
        services.AddScoped<IConversationRepository, ConversationRepository>();

        var profileApi = configuration["Services:ProfileApi"] ?? "http://profile-api:8080";
        services.AddHttpClient<IChatGroupClient, HttpChatGroupClient>(c => c.BaseAddress = new Uri(profileApi));
        services.AddHttpClient<IPlayerInfoClient, HttpPlayerInfoClient>(c => c.BaseAddress = new Uri(profileApi));

        var encryptionKey = configuration["Chat:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(encryptionKey))
            encryptionKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        services.AddSingleton<IMessageEncryptionService>(new AesMessageEncryptionService(encryptionKey));

        return services;
    }
}
