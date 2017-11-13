using Microsoft.Extensions.Configuration;

namespace BalticAmadeus.Config
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddCloudConfig(this IConfigurationBuilder builder, string serviceName)
        {
            AddCloudConfig(builder, serviceName, required: true);
            return builder;
        }

        public static IConfigurationBuilder AddCloudConfig(this IConfigurationBuilder builder, string serviceName, bool required)
        {
            builder.Add(new BaConfigSource(serviceName, required));
            return builder;
        }
    }
}
