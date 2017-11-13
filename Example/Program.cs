using BalticAmadeus.Config;
using Microsoft.Extensions.Configuration;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCloudConfig("configss", true)
                .Build();
        }
    }
}
