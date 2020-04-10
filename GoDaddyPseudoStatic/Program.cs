namespace GoDaddyPseudoStatic
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddUserSecrets<WorkerSecrets>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    var options = configuration.GetSection("Worker").Get<WorkerOptions>();
                    services.AddSingleton(options);

                    var secrets = configuration.GetSection("WorkerSecrets").Get<WorkerSecrets>();
                    services.AddSingleton(secrets);

                    services.AddLogging(opt =>
                    {
                        opt.AddConsole(c => c.TimestampFormat = "[HH:mm:ss] ");
                    });

                    services.AddHostedService<Worker>();
                });
    }
}
