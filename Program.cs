using System;
using System.Threading.Tasks;
using System.Collections;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace DeathGameAssistant
{
    class Program
    {
        public static DiscordClient discord;
        public static CommandsNextExtension cmd;
        static InteractivityExtension interactivity;
        static Dictionary<ulong, DeathGame> DeathGames = new Dictionary<ulong, DeathGame>();
        static List<DiscordMessage> WatchedGameMessages = new List<DiscordMessage>();
        static void Main(string[] args) => new Program().MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        //{
        //    MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        //}

        public async Task MainAsync(string[] args)
        {
            SettingsManager.LoadSettings();
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = SettingsManager.Cfg.Token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information
            });

            var deps = new ServiceCollection()
                .AddSingleton(DeathGames)
                .AddSingleton(WatchedGameMessages)
                .BuildServiceProvider();

            cmd = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = SettingsManager.Cfg.CommandPrefixes,
                EnableDms = true,
                EnableMentionPrefix = true,
                Services = deps
            });
            interactivity = discord.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Ignore,


                // default timeout for other actions to 2 minutes
                Timeout = TimeSpan.FromMinutes(2)
            });

            discord.Ready += async e =>
            {
                LogToConsole(LogLevel.Info, "Assistant Ready");
            };

            cmd.CommandExecuted += Commands_CommandExecuted;
            cmd.CommandErrored += Commands_CommandErrored;
            discord.MessageReactionAdded += DeathGame_StartMsg_MessageReactionAdded;
            discord.MessageReactionRemoved += DeathGame_StartMsg_MessageReactionRemoved;

            discord.GuildCreated += Discord_GuildCreated;


            cmd.RegisterCommands<DeathGameCommands>();
            cmd.RegisterCommands<ConfigCommands>();
            cmd.RegisterCommands<CharacterCommands>();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        public static void LogToConsole(LogLevel level, string message)
        {
            discord.DebugLogger.LogMessage(level, "DGAssistant", message, DateTime.Now);
        }
        private Task Discord_GuildCreated(DSharpPlus.EventArgs.GuildCreateEventArgs e)
        {
            discord.DebugLogger.LogMessage(LogLevel.Info, "DGAssistant", "Joined new server, creating default config", DateTime.Now);
            ServerSetting defset = new ServerSetting { AnnounceChannelId = e.Guild.GetDefaultChannel().Id };
            SettingsManager.Cfg.ServerSettings.Add(e.Guild.Id, defset);
            return Task.CompletedTask;
        }

        private async Task DeathGame_StartMsg_MessageReactionAdded(MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot)
                return;
            if (WatchedGameMessages.Contains(e.Message))
            {
                Task.Run(() => AddGameParticipant(e));
            }
        }

        static async Task AddGameParticipant(MessageReactionAddEventArgs e)
        {
            var member = await e.Guild.GetMemberAsync(e.User.Id);
            await member.SendMessageAsync("Por favor, dime el nombre de tu personaje (Tienes 1 minuto)");
            var reply = await interactivity.WaitForMessageAsync(x => x.Channel.IsPrivate == true && x.Author.Id == e.User.Id, TimeSpan.FromSeconds(60));
            if (reply.Result != null)
            {
                string name = reply.Result.Content;
                DeathGames[e.Guild.Id].AddParticipant(member, name);
                await member.SendMessageAsync($"Bienvenido al Juego, {name}!");
                var pchannel = e.Guild.GetChannel(SettingsManager.GetGuildSettings(e.Guild).FMasterChannelId);
                await pchannel.SendMessageAsync($"Se ha sumado {name} ({e.User.Username}#{e.User.Discriminator}) al juego activo.");
            }
            else
            {
                await member.SendMessageAsync("Vuelve a añadir tu reacción al mensaje, y comienza de nuevo.");
            }
        }

        private async Task DeathGame_StartMsg_MessageReactionRemoved(MessageReactionRemoveEventArgs e)
        {
            if (e.User.IsBot)
                return;
            if (WatchedGameMessages.Contains(e.Message) && DeathGames.ContainsKey(e.Guild.Id))
            {
                var dg = DeathGames[e.Guild.Id];
                var part = dg.participants.First(x => x.Id == e.User.Id);
                var pchannel = e.Guild.GetChannel(SettingsManager.GetGuildSettings(e.Guild).FMasterChannelId);
                await pchannel.SendMessageAsync($"Se ha ido {part.Name} ({e.User.Username}#{e.User.Discriminator}) del juego activo.");
                dg.participants.Remove(part);
            }
        }

        private static Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            // let's log the name of the command and user
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "WBot2", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private static async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            // let's log the error details
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "WBot2", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "WBot2", "Stack Trace: \n" + e.Exception.StackTrace, DateTime.Now);
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
                    // there are also some pre-defined colors available
                    // as static members of the DiscordColor struct
                };
                await e.Context.RespondAsync("", embed: embed);
            }
            if (e.Exception is CommandNotFoundException ex2)
            {
                // let's wrap the response into an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Sumimasen",
                    Description = $"{ex2.CommandName} is not recognized as a command.",
                    Color = DiscordColor.DarkRed // red
                    // there are also some pre-defined colors available
                    // as static members of the DiscordColor struct
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }
    }

}