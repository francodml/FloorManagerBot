using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;

namespace DeathGameAssistant
{
    static class SettingsManager
    {
        public static Config Cfg;
        private static string ConfigFile = Directory.GetCurrentDirectory() + "\\config.json";
        private static string OldSuffix = ".old";

        public static void LoadSettings()
        {
            if (File.Exists(ConfigFile))
            {
                string json = File.ReadAllText(ConfigFile);
                Cfg = JsonConvert.DeserializeObject<Config>(json);
            }
            else
            {
                Console.WriteLine("No config found. Creating default.");
                Console.WriteLine("Enter bot access token");
                string token = Console.ReadLine();
                Cfg = new Config
                {
                    Token = token,
                    CommandPrefixes = new[] { ":" },
                    ServerSettings = new Dictionary<ulong, ServerSetting>()
                };
            }
            SaveSettings();
        }

        public static void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(Cfg);
            if (File.Exists(ConfigFile))
            {
                if (File.Exists(ConfigFile + OldSuffix))
                    File.Delete(ConfigFile + OldSuffix);
                File.Move(ConfigFile, ConfigFile + OldSuffix);
            }
            File.WriteAllText(ConfigFile, json);
        }

        public static ServerSetting GetGuildSettings(DiscordGuild guild)
        {
            if (Cfg.ServerSettings.ContainsKey(guild.Id))
            {
                return Cfg.ServerSettings[guild.Id];
            }
            else
            {
                ServerSetting defset = new ServerSetting
                {
                    AnnounceChannelId = guild.GetDefaultChannel().Id,
                    FMasterChannelId = guild.GetDefaultChannel().Id
                };
                Cfg.ServerSettings.Add(guild.Id, defset);
                SaveSettings();
                return Cfg.ServerSettings[guild.Id];
            }
        }

        public static void SetAnnounceChannel(CommandContext ctx)
        {
            GetGuildSettings(ctx.Guild).AnnounceChannelId = ctx.Channel.Id;
            SaveSettings();
        }
        public static DiscordChannel GetAnnounceChannel(CommandContext ctx)
        {
            return ctx.Guild.GetChannel(GetGuildSettings(ctx.Guild).AnnounceChannelId);
        }

        public static void SetFMasterChannel(CommandContext ctx)
        {
            GetGuildSettings(ctx.Guild).FMasterChannelId = ctx.Channel.Id;
            SaveSettings();
        }
        public static DiscordChannel GetFMasterChannel(CommandContext ctx)
        {
            return ctx.Guild.GetChannel(GetGuildSettings(ctx.Guild).FMasterChannelId);
        }

    }
    public class Config
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("prefixes")]
        public string[] CommandPrefixes { get; set; }

        [JsonProperty("serversettings")]
        //public List<ServerSetting> ServerSettings { get; set; }
        public Dictionary<ulong,ServerSetting> ServerSettings { get; set; }
    }

    public class ServerSetting
    {
        public ulong AnnounceChannelId { get; set; }
        public ulong FMasterChannelId { get; set; }
    }
}
