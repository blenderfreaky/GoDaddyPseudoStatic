namespace GoDaddyPseudoStatic
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration(builder => builder.AddUserSecrets<WorkerSecrets>())
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    var options = JsonSerializer.Deserialize<WorkerOptions>(configuration.GetSection("Worker").ToJson(), new JsonSerializerOptions { IncludeFields = true, Converters = { new TimeSpanConverter() } });
                    services.AddSingleton(options);

                    var secrets = configuration.GetSection("WorkerSecrets").Get<WorkerSecrets>();
                    services.AddSingleton(secrets);

                    services.AddLogging(opt => opt.AddConsole(c => c.TimestampFormat = "[HH:mm:ss] "));

                    services.AddHostedService<Worker>();
                });
    }
}