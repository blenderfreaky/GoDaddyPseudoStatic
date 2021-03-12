namespace GoDaddyPseudoStatic
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text.Json;

    public static class Program
    {
        public static void Main(string[] args)
        {
            // This needs to be set before anything else happens so that the Hosting stuff finds the appsettings.json file
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration(builder => builder.AddJsonFile("appsettings.apikey.json"))
                .ConfigureLogging(loggerFactory => loggerFactory.AddEventLog().AddSimpleConsole(x => x.TimestampFormat = "[HH:mm:ss] "))
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    var optionsJson = configuration.GetSection("Worker").ToJson();
                    var options = JsonSerializer.Deserialize<WorkerOptions>(optionsJson, new JsonSerializerOptions { IncludeFields = true, Converters = { new TimeSpanConverter() } });
                    services.AddSingleton(options);

                    var secrets = configuration.GetSection("WorkerSecrets").Get<WorkerSecrets>();
                    services.AddSingleton(secrets);

                    services.AddHostedService<Worker>();
                });
    }
}