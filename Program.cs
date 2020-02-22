using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading;
using Winton.Extensions.Configuration.Consul;

namespace ConsulConsoleIOpts
{
    class Program
    {
        static void Main()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider provider = services.BuildServiceProvider();
            App app = provider.GetService<App>();
            do
            {
                app.Run();
                Thread.Sleep(100);
            } while (true);
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
            services.AddLogging(builder => builder.AddConsole());
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"appsettings.json", optional: false, reloadOnChange: true)
                .AddConsul("appsettings.json", opts => {
                    opts.ConsulConfigurationOptions = cco => { cco.Address = new System.Uri("http://localhost:8500"); };
                    opts.Optional = true;
                    opts.ReloadOnChange = true;
                    opts.OnLoadException = exc =>
                    {
                        exc.Ignore = true;
                        logger.LogInformation("Unable to connect to consul");
                    };
                })
                .AddEnvironmentVariables()
                .Build();

            services.AddOptions();
            services.Configure<AppSettings>(configuration.GetSection("App"));
            services.AddSingleton<App>();
        }
    }

    public class App
    {
        private readonly ILogger _logger;
        private string _name;

        public App(IOptionsMonitor<AppSettings> options, ILogger<App> logger)
        {
            _name = options.CurrentValue.Name;
            options.OnChange<AppSettings>(settings =>
            {
                _name = settings.Name;
            });
            _logger = logger;
        }

        public void Run()
        {
            _logger.LogInformation("Name: {Name}", _name);
        }
    }
}
