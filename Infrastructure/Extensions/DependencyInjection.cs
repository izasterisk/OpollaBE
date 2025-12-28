using BLL.Interfaces.Infrastructure;
using Infrastructure.Authentication;
using Infrastructure.Classes;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Add HttpClient factory
        services.AddHttpClient();

        // Register Infrastructure services
        services.AddScoped<ApiHelper>();
        services.AddScoped<ILoginApi, LoginApi>();
        services.AddScoped<IProfileApi, ProfileApi>();
        services.AddScoped<IClassApi, ClassApi>();

        return services;
    }
}
