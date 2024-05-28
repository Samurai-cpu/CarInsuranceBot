using CarInsuranceBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CarInsuranceBot.Intefaces;
using CarInsuranceBot.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CarInsuranceBot
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var botRunner = scope.ServiceProvider.GetRequiredService<IBotRunner>();
                botRunner.Run();
            }

            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .UseConsoleLifetime()
               .ConfigureLogging(builder => 
               {
                   builder.SetMinimumLevel(LogLevel.Warning);
                   builder.AddConsole();
                   builder.AddDebug();
               })
               .ConfigureAppConfiguration((hostingContext, config) =>
               {
                   config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                   config.AddEnvironmentVariables();
               })
               .ConfigureServices((hostContext, services) =>
               {
                   IConfiguration configuration = hostContext.Configuration;
                   

                   services.AddDbContext<AppDbContext>(options =>
                       options.UseSqlServer(configuration["ConnectionString"]));
                   services.AddHttpClient<IMindeeService, MindeeService>(options =>
                   {
                       options.BaseAddress = new Uri(configuration.GetSection("Mindee")["baseUrl"]!);
                   })
                   .ConfigurePrimaryHttpMessageHandler(() =>
                   {
                       //I was set auto redirect to false because httpClient cleans auth header before redirect
                       //So with auto redirect we will continiously get 401 error from mindee api
                       return new HttpClientHandler
                       {
                           AllowAutoRedirect = false
                       };
                   });
                   services.AddSingleton<IBotRunner, BotRunner>();
                   services.AddSingleton<ICustomerService, CustomerService>();
               });
    }
}