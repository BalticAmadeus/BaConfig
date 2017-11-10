using Microsoft.Extensions.Configuration;

namespace BalticAmadeus.Config
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddCloudConfig(this IConfigurationBuilder builder, string applicationName, string serviceName)
        {
            builder.Add(new BaConfigSource(applicationName));
            builder.Add(new BaConfigSource(serviceName));
            return builder;
        }
    }
}
