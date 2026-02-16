using Hermod.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hermod.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHermodCore(this IServiceCollection services)
    {
        services.AddScoped<PlayService>();
        services.AddScoped<UserService>();
        services.AddScoped<GroupService>();
        services.AddScoped<PlayerMappingService>();
        return services;
    }
}
