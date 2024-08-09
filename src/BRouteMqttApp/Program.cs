using HomeAssistantAddOn.Core;

namespace BRouteMqttApp;

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddHomeAssistantAddOnConfig();
            })
            .ConfigureLogging((context, loggingBuilder) =>
            {
                var config = context.Configuration.Get<CommonOptions>();
                loggingBuilder
                .AddFilter(string.Empty, config!.LogLevel)
                .AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy/MM/dd HH:mm:ss ";
                });

            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<Worker>();
                services.AddHomeAssistantMqtt();
                services.AddBRouteController();
            })
            .Build();

        host.Run();
    }
}