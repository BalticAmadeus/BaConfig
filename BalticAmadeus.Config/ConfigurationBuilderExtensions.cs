using System.IO;
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
        
        public static IConfigurationBuilder AddServiceConfigs(this IConfigurationBuilder builder, string serviceName)
        {
            var serviceBlobName = serviceName + ".json";
            var serviceSecretBlobName = serviceName + "Secrets.json";

            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddCloudConfig("GlobalSettings.json")
                .AddCloudConfig("GlobalSettingsSecrets.json")
                .AddCloudConfig(serviceBlobName)
                .AddCloudConfig(serviceSecretBlobName);

            return builder;
        }
    }
}
