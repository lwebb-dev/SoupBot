using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SoupBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService commandService;
        private readonly DiscordSocketClient discordSocketClient;
        private readonly IServiceProvider serviceProvider;

        public CommandHandlingService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.commandService = this.serviceProvider.GetRequiredService<CommandService>();
            this.discordSocketClient = this.serviceProvider.GetRequiredService<DiscordSocketClient>();

            this.commandService.CommandExecuted += CommandExecutedAsync;
            this.discordSocketClient.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inhereit ModuleBase<T>.
            await this.commandService.AddModulesAsync(Assembly.GetEntryAssembly(), this.serviceProvider);
        }

        public async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            // This prefix defines the user's message as a valid command.
            // example: .help
            const char commandPrefix = '.';

            // Ignore non-SocketUserMessages.
            if (!(socketMessage is SocketUserMessage userMessage))
                return;

            // Ignore system messages and messages from other bots.
            if (userMessage.Source != MessageSource.User)
                return;

            // Perform prefix check on commandPrefix
            int argPos = 0;
            if (!userMessage.HasCharPrefix(commandPrefix, ref argPos))
                return;

            // Perform the execution of the command.
            // In this method, commandService will perform precondition and parsing checks,
            // then execute the command if one is matched.
            SocketCommandContext context = new SocketCommandContext(this.discordSocketClient, userMessage);
            await this.commandService.ExecuteAsync(context, argPos, this.serviceProvider);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // Occurs when a search fails (IE: Command not found). 
            // These failures can simply be bypassed.
            if (!command.IsSpecified)
                return;

            // Command was successful. We normally don't need to log success results.
            if (result.IsSuccess)
                return;

            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}
