using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;

namespace DeathGameAssistant
{
    public class DeathGame
    {
        public GameState State { get; private set; } = GameState.Waiting;
        public List<Participant> participants = new List<Participant>();
        public ulong GuildId;
        public DiscordMember MainFM;

        public void ChangeState(GameState state)
        {
            State = state;
        }
        
        public Participant AddParticipant(DiscordMember member, string name)
        {
            Participant newpart = new Participant(member, name);
            newpart.Tokens.Add(new GameToken(newpart, 100));
            participants.Add(newpart);
            return newpart;
        }

        public Participant ParticipantFromMember(DiscordMember member)
        {
            Participant part;
            try
            {
                part = this.participants.First(x => x.Id == member.Id);
                return part;
            }
            catch (Exception)
            {
                Program.LogToConsole(LogLevel.Error, "Somehow you've fucked up.");
                return null;
            }
        }
    }

    public static class GameUtils
    {
        public static DeathGame GameFromGuild(DiscordGuild guild)
        {
            var deathGames = (Dictionary<ulong, DeathGame>)Program.cmd.Services.GetService(typeof(Dictionary<ulong, DeathGame>));
            return deathGames.First(x => x.Key == guild.Id).Value;
        }

        public static async Task<bool> CheckFloormaster(DeathGame dg, CommandContext ctx)
        {
            var executor = ctx.Member;
            if (dg.MainFM.Id == executor.Id)
            {
                return true;
            }
            else
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder { Title = "Restringido a Floor Masters", Description = "Solo el Floor Master puede hacer eso." });
                return false;
            }
        }

    }
}
