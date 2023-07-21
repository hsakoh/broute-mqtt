using BRouteController;
using EchoDotNetLite;
using EchoDotNetLiteSkstackIpBridge;
using Microsoft.Extensions.Configuration;
using SkstackIpDotNet;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddBRouteController(
        this IServiceCollection services)
    {
        services.AddSingleton<SKDevice>();
        services.AddSingleton<SkstackIpPANAClient>();
        services.AddSingleton<IPANAClient>(_ => _.GetRequiredService<SkstackIpPANAClient>());
        services.AddSingleton<EchoClient>();

        services.AddOptions<BRouteOptions>().
            Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("BRoute").Bind(settings);
            });
        services.AddSingleton<BRouteControllerService>();
        return services;
    }
}
