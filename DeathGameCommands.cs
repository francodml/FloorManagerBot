using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;

namespace DeathGameAssistant
{
    [Group("deathgame")]
    [Aliases("dg", "game")]
    [Description("Comandos para manejar instancias de Death Games")]
    class DeathGameCommands : BaseCommandModule
    {
        public Dictionary<ulong,DeathGame> DeathGames { get; }
        public List<DiscordMessage> WatchedStartMessages { get; }
        public DeathGameCommands(Dictionary<ulong, DeathGame> dgs, List<DiscordMessage> wsm)
        {
            DeathGames = dgs;
            WatchedStartMessages = wsm;
        }
        [Command("create")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task Create(CommandContext ctx)
        {
            if (DeathGames.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("This server is already running a Death Game.");
                return;
            }
            await ctx.RespondAsync($"Creating Game Instance for guild {ctx.Guild.Name} (guild id: {ctx.Guild.Id}).\nYou have been assigned as Floor Master.");
            DeathGame deathGame = new DeathGame
            {
                MainFM = ctx.Member
            };
            DeathGames.Add(ctx.Guild.Id,deathGame);
            Program.LogToConsole(LogLevel.Info, $"A death game has been created for guild {ctx.Guild.Name} id {ctx.Guild.Id}\nMain Floor Master is {ctx.Member.Username}#{ctx.Member.Discriminator}");
            var embed = new DiscordEmbedBuilder
            {
                Title = "Se ha creado un nuevo Juego de Muerte!",
                Description = "Para participar, agregue una reacción a este mensaje."
            };
            var msg = await SettingsManager.GetAnnounceChannel(ctx).SendMessageAsync(embed: embed);
            await msg.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":blue_circle:"));
            WatchedStartMessages.Add(msg);
        }
        [Command("stop")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task Stop(CommandContext ctx)
        {
            if (!DeathGames.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("This server is not running a Death Game");
                return;
            }
            DeathGame dg = DeathGames[ctx.Guild.Id];
            var fmcheck = await GameUtils.CheckFloormaster(dg, ctx);
            if (fmcheck)
            {
                await ctx.RespondAsync("Stopping server's active Death Game instance.");
                DeathGames.Remove(ctx.Guild.Id);
                var watchedmsg = WatchedStartMessages.First(x => x.Channel.Guild.Id == ctx.Guild.Id);
                if (WatchedStartMessages.Contains(watchedmsg))
                    WatchedStartMessages.Remove(watchedmsg);
            }
        }
        [Command("start")]
        public async Task StartGame(CommandContext ctx)
        {
            if (!DeathGames.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("This server is not running a Death Game");
                return;
            }
            DeathGame dg = DeathGames[ctx.Guild.Id];
            var fmcheck = await GameUtils.CheckFloormaster(dg, ctx);
            if (fmcheck)
            {
                await ctx.RespondAsync("Iniciando el Juego...");
                WatchedStartMessages.Remove(WatchedStartMessages.First(x => x.Channel.Guild.Id == ctx.Guild.Id));
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Comienza el Juego de Muerte!",
                    Description = "A partir de ahora, nadie puede sumarse."
                };
                var msg = await SettingsManager.GetAnnounceChannel(ctx).SendMessageAsync(embed: embed);
                DeathGames[ctx.Guild.Id].ChangeState(GameState.Preparation);
            }
        }

        [Command("beginvote")]
        public async Task BeginVote(CommandContext ctx)
        {
            if (!DeathGames.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("This server is not running a Death Game");
                return;
            }
            DeathGame dg = DeathGames[ctx.Guild.Id];
            var fmcheck = await GameUtils.CheckFloormaster(dg, ctx);
            if (fmcheck)
            {
                dg.ChangeState(GameState.Vote);
                //
                var announceChannel = SettingsManager.GetAnnounceChannel(ctx);
                string partpList = "";
                foreach (Participant participant in dg.participants)
                {
                    string listItem = dg.participants.IndexOf(participant).ToString() + " - " + participant.Name+ "\n";
                    partpList += listItem;
                }
                await announceChannel.SendMessageAsync("Comienza una votación por mayoría!\nLos participantes por favor, envíen sus votos acorde a la siguiente tabla:");
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Lista de participantes",
                    Description = partpList,
                };
                await announceChannel.SendMessageAsync(embed: embed);
            }

        }
        [Command("info")]
        public async Task Info(CommandContext ctx)
        {
            if (!DeathGames.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("This server is not running a Death Game");
                return;
            }
            DeathGame dg = DeathGames[ctx.Guild.Id];
            string partpList = "";
            foreach (Participant participant in dg.participants)
            {
                string listItem = dg.participants.IndexOf(participant).ToString() + " - " + participant.Name + "\n";
                partpList += listItem;
            }
            var embed = new DiscordEmbedBuilder
            {
                Title = $"Juego de Muerte en {ctx.Guild.Name}",
                Description = 
                $@"Main FloorMaster: {dg.MainFM.Nickname}
Estado del juego: {dg.State}
Participantes: 
{partpList}"
            };

            await ctx.RespondAsync(embed: embed);
        }
    }
    [Group("me")]
    [Description("Comandos relacionados a tu personaje en el juego")]
    class CharacterCommands : BaseCommandModule
    {
        public Dictionary<ulong, DeathGame> DeathGames { get; }

        public CharacterCommands(Dictionary<ulong, DeathGame> dgs)
        {
            DeathGames = dgs;
        }

        [Command("info")]
        public async Task CharInfo(CommandContext ctx)
        {
            if (!DeathGames.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("This server is not running a Death Game");
                return;
            }
            DeathGame dg = DeathGames[ctx.Guild.Id];
            Participant curPar = dg.ParticipantFromMember(ctx.Member);
            string tokenList = "";
            foreach (GameToken token in curPar.Tokens)
            {
                tokenList += token.ToString() + "\n";
            }
            var embed = new DiscordEmbedBuilder
            {
                Title = curPar.Name,
                Description = $"Rol: {curPar.Role}\nTokens\n {tokenList}"
            };
            await ctx.RespondAsync(embed: embed);
        }

    }

    [Group("config")]
    [Description("Configuración pertinente al servidor")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    class ConfigCommands : BaseCommandModule
    {
        [Command("mainchannel")]
        [Description("Utilizar el canal actual para enviar mensajes públicos relacionados con el Death Game")]
        public async Task SetAnnounceChannel(CommandContext ctx)
        {
            SettingsManager.SetAnnounceChannel(ctx);
            await ctx.RespondAsync("Los mensajes públicos de los DG se enviarán en este canal.");
        }
        [Command("fmasterchannel")]
        [Aliases("fmchannel")]
        public async Task SetFMChannel(CommandContext ctx)
        {
            SettingsManager.SetFMasterChannel(ctx);
            await ctx.RespondAsync("Los mensajes de Floor Master se enviarán a este canal.");
        }
    }
}
