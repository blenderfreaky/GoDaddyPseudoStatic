namespace GoDaddyPseudoStatic
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
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
                .ConfigureAppConfiguration(builder => builder.AddUserSecrets<WorkerSecrets>())
                .ConfigureLogging(loggerFactory => loggerFactory.AddEventLog().AddSimpleConsole(x => x.TimestampFormat = "[HH:mm:ss] "))
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    var optionsJson = configuration.GetSection("Worker").ToJson();

                    File.WriteAllText("C:\\Users\\blend\\dbg2", $"{string.Join(", ", configuration.GetChildren().Select(x => x.Key))}");
                    File.AppendAllText("C:\\Users\\blend\\dbg2", $"\n{string.Join(", ", configuration.GetSection("Worker").GetChildren().Select(x => x.Key))}");
                    File.AppendAllText("C:\\Users\\blend\\dbg2", $"\n{Environment.CurrentDirectory}");
                    File.WriteAllText("C:\\Users\\blend\\dbg.json", Encoding.UTF8.GetString(optionsJson));
                    var options = JsonSerializer.Deserialize<WorkerOptions>(optionsJson, new JsonSerializerOptions { IncludeFields = true, Converters = { new TimeSpanConverter() } });
                    services.AddSingleton(options);

                    var secrets = configuration.GetSection("WorkerSecrets").Get<WorkerSecrets>();
                    services.AddSingleton(secrets);

                    services.AddHostedService<Worker>();
                });
    }
}