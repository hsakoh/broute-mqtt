namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddHomeAssistantAddOnConfig(this IConfigurationBuilder builder)
    {
        return builder.AddJsonFile("/data/options.json", optional: true);
    }
}