namespace Comeback.BuildingBlocks.Infrastructure.Media;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class CloudinaryServiceCollectionExtensions
{
    public static IServiceCollection AddCloudinary(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.AddHttpClient<ICloudinaryMediaService, CloudinaryMediaService>();

        return services;
    }
}
