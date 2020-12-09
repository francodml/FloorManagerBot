using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;

namespace DeathGameAssistant
{
    public enum GameState
    {
        Waiting,
        Preparation,
        Exploration,
        Maingame,
        Vote
    }

    public enum GameRole
    {
        Commoner,
        Keymaster,
        Sage,
        Sacrifice,
        FloorMaster
    }
    public class Participant
    {
        public DiscordMember Member { get; private set; }
        public string Name;
        public ulong Id { get; private set; }
        public ulong GameID { get; private set; }
        public GameRole Role = GameRole.Commoner;
        public bool Alive = true;
        public bool IsLeader = false;
        public List<GameToken> Tokens = new List<GameToken>();

        public Participant(DiscordMember member, string name, GameRole role = GameRole.Commoner, bool leader = false)
        {
            Member = member;
            Name = name;
            Id = member.Id;
            GameID = member.Guild.Id;
            Role = role;
            IsLeader = leader;
        }

        public int GiveTokens(Participant participant, int num)
        {
            GameToken selected = Tokens.First(x => x.Id == participant.Id);
            selected.Amount += num;
            return selected.Amount;
        }
        public int DeductTokens(Participant participant, int num)
        {
            GameToken selected = Tokens.First(x => x.Id == participant.Id);
            if (selected.Amount - num <= 0)
            {
                selected.Amount = 0;
                return 0;
            }
            selected.Amount -= num;
            return selected.Amount;
        }
    }

    public struct GameToken
    {
        public ulong Id { get; private set; }
        public string Name { get; private set; }
        public int Amount;

        public GameToken(Participant participant, int amount)
        {
            Id = participant.Id;
            Name = participant.Name;
            Amount = amount;
        }

        public override string ToString()
        {
            var concatString = $"{this.Name}: {this.Amount}\n";
            return concatString;
        }
    }
}
