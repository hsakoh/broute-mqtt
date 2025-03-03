using BRouteController;
using EchoDotNetLite;
using EchoDotNetLiteSkstackIpBridge;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkstackIpDotNet;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddBRouteController(
        this IServiceCollection services)
    {
        services.AddSingleton<ISKDevice>(_ =>
        {
            var options = _.GetRequiredService<IOptions<BRouteOptions>>();
            if (options.Value.UseBP35C0Commands)
            {
                var logger = _.GetRequiredService<ILogger<SKDeviceBP35C0>>();
                return new SKDeviceBP35C0(logger);
            }
            else
            {
                var logger = _.GetRequiredService<ILogger<SKDeviceBP35A1>>();
                return new SKDeviceBP35A1(logger);
            }
        });
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
