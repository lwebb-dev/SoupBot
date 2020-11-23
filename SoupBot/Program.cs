using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SoupBot.Services;
using System.IO;

namespace SoupBot
{
    public class Program
    {
        public static IConfiguration Configuration;

        public static void Main()
        {
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string baseDir = AppContext.BaseDirectory;
            string slnDir = baseDir.Substring(0, baseDir.LastIndexOf("SoupBot"));
            string projDir = Path.Combine(slnDir, "SoupBot");

            Configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(projDir, "appsettings.json"), optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine(projDir, $"appsettings.{env}.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            string token = Configuration["DISCORD_BOT_TOKEN"];

            using (ServiceProvider serviceProvider = this.ConfiguredServices())
            {
                DiscordSocketClient discordSocketClient = serviceProvider.GetRequiredService<DiscordSocketClient>();

                discordSocketClient.Log += LogAsync;
                serviceProvider.GetRequiredService<CommandService>().Log += LogAsync;

                // Login and start bot.
                await discordSocketClient.LoginAsync(TokenType.Bot, token);
                await discordSocketClient.StartAsync();

                // Initialize Middleware services
                await serviceProvider.GetRequiredService<CommandHandlingService>().InitializeAsync();

                // Block the program until it is closed.
                await Task.Delay(Timeout.Infinite);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private ServiceProvider ConfiguredServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }
    }
}
