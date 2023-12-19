using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;

namespace DSPlus.LethalQuestsBot
{
    public class LethalQuestsBot
    {
        public DiscordClient Client { get; set; }
        public InteractivityExtension Interactivity { get; set; }
        public CommandsNextExtension Commands { get; set; }
        public SlashCommandsExtension SlashCommands { get; set; }
        public readonly EventId BotEventId = new EventId(42, "LethalQuestBot");
        public readonly IConfiguration _configuration;
        public readonly string _prefix;
        public readonly bool _allowOtherBots;

        public LethalQuestsBot(IServiceProvider services, IConfiguration configuration)
        {
            _configuration = configuration;
            _prefix = _configuration.GetValue<string>("prefix");
            _allowOtherBots = _configuration.GetValue<bool>("AllowOtherBots");

            var cfg = new DiscordConfiguration
            {
                Token = _configuration.GetValue<string>("token"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                LogUnknownEvents = false
            };

            // then we want to instantiate our client
            this.Client = new DiscordClient(cfg);

            // next, let's hook some events, so we know
            // what's going on
            this.Client.Ready += this.Client_Ready;
            this.Client.GuildAvailable += this.Client_GuildAvailable;
            this.Client.ClientErrored += this.Client_ClientError;

            // let's enable interactivity, and set default options
            this.Client.UseInteractivity(new InteractivityConfiguration
            {
                // default pagination behaviour to just ignore the reactions
                PaginationBehaviour = PaginationBehaviour.Ignore,

                // default timeout for other actions to 2 minutes
                Timeout = TimeSpan.FromMinutes(2)
            });

            var scfg = new SlashCommandsConfiguration
            {
                Services = services
            };

            this.SlashCommands = this.Client.UseSlashCommands(scfg);

            // let's hook some command events, so we know what's
            // going on

            this.SlashCommands.SlashCommandExecuted += this.SlashCommands_SlashCommandExecuted;
            this.SlashCommands.SlashCommandErrored += this.SlashCommands_SlashCommandErrored;

            // up next, let's register our commands
            this.SlashCommands.RegisterCommands<BotCommands>();

            // finally, let's connect and log in
            this.Client.ConnectAsync();
        }

        private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            // let's log the fact that this event occured
            sender.Logger.LogInformation(BotEventId, "Client is ready to process events.");

            DiscordActivity DA = new DiscordActivity(_configuration.GetValue<string>("prefix") + " help", ActivityType.Watching);
            sender.UpdateStatusAsync(DA);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            // let's log the name of the guild that was just
            // sent to our client
            sender.Logger.LogInformation(BotEventId, $"Guild available: {e.Guild.Name}");

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            // let's log the details of the error that just
            // occured in our client
            sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private Task SlashCommands_SlashCommandExecuted(SlashCommandsExtension sender, SlashCommandExecutedEventArgs e)
        {
            // let's log the name of the command and user
            e.Context.Client.Logger.LogInformation(BotEventId, $"{e.Context.User.Username} successfully executed '{e.Context.QualifiedName}'");

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }
        
        private async Task SlashCommands_SlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
            // let's log the error details
            string fullCommand = e.Context.CommandName;
            e.Context.Client.Logger.LogError(BotEventId, $"{e.Context.User.Username} tried executing '{fullCommand}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            // let's check if the error is a result of lack
            // of required permissions
            if (e.Exception is ChecksFailedException ex)
            {
                // yes, the user lacks required permissions,
                // let them know

                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            }
            else
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
                string title;
                string message = "";

                if (fullCommand.StartsWith("!"))
                {
                    title = "Use Slash Commands";
                    message = $"{emoji} Only slash commands are available now!\nPlease see <#819551634103336990> for more information!";
                }
                else
                {
                    title = "Invalid Command";
                    message = $"{emoji} It appears you used an invalid command or must be used in a direct message only!";
                }

                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = title,
                    Description = message,
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            }
        }
    }
}