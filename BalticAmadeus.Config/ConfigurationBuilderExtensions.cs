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
        
        public static IConfigurationBuilder AddStructuredCloudConfig(this IConfigurationBuilder builder, string serviceName)
        {
            var serviceBlobName = serviceName + ".json";
            var serviceSecretBlobName = serviceName + ".secrets.json";

            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddCloudConfig("globalSettings.json")
                .AddCloudConfig("globalSettings.secrets.json")
                .AddCloudConfig(serviceBlobName)
                .AddCloudConfig(serviceSecretBlobName);

            return builder;
        }
    }
}
