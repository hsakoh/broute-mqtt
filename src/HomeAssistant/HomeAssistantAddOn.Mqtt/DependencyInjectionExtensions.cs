using HomeAssistantAddOn.Mqtt;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddHomeAssistantMqtt(
        this IServiceCollection services)
    {
        services.AddOptions<MqttOptions>().
            Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("Mqtt").Bind(settings);
            });
        services.AddSingleton<MqttService>();
        return services;
    }
}
