using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace RequestMonitor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>{
                    config.Sources.Clear();
                    config.AddJsonFile($"appsettings.json", false);
                    config.AddEnvironmentVariables();
                })
                .UseStartup<Startup>()
                .UseKestrel()
                .Build();
    }
}
