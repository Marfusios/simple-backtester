using System;
using Microsoft.Extensions.Configuration;

namespace RangeBarProfit
{
    public class ConfigUtils
    {
        public static IConfigurationRoot InitConfig(string environment)
        {
            // var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
            var path = AppContext.BaseDirectory;
            Console.WriteLine($"[CONFIG] Searching configuration at location: {path}");

            var env = environment ?? Environment.GetEnvironmentVariable("SIMPLE_BACKTESTER_ENV");
            Console.WriteLine($"[CONFIG] Currently selected environment: {env}");

            var builder = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        public static void FillConfig<T>(IConfigurationRoot root, string section, T config)
        {
            root?.GetSection(section).Bind(config);
        }
    }
}
