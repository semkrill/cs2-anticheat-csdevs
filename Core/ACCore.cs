using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using TBAntiCheat.Handlers;
using CounterStrikeSharp.API.Core.Capabilities;
using TBAntiCheat.Detections.Modules;
using TBAntiCheat.Detections;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;

namespace TBAntiCheat.Core
{
    [MinimumApiVersion(234)]
    public class ACCore : BasePlugin
    {
        public override string ModuleName => "TB Anti-Cheat";
        public override string ModuleVersion => "0.6.0";
        public override string ModuleAuthor => "SemKrill & Killer_bigpoint";
        public override string ModuleDescription => "Proper Anti-Cheat for CS2";

        private static ACCore core;
        private static ILogger? logger = null;
        private static BaseConfig<CoreCfgSaveData> coreCfg;

        public override void Load(bool hotReload)
        {
            core = this;
            logger = Logger;

            Globals.PreInit(this);

            CommandHandler.InitializeCommandHandler(this);

            EventListeners.InitializeListeners(this);
            EventHandlers.InitializeHandlers(this, hotReload);

            coreCfg = new BaseConfig<CoreCfgSaveData>("Core");

            Log($"[TBAC] Loaded (v{ModuleVersion})");
        }

        internal static void Log(string message)
        {
            if (logger == null)
            {
                return;
            }

            logger.Log(LogLevel.Information, message);
        }

        internal static ACCore GetCore() => core;
        internal static string GetBanReason() => coreCfg.Config.BanReason;
        internal static bool GetIsPrintToReasinCheatInfo() => coreCfg.Config.IsPrintCheatToReason;
        internal static bool GetIsPrintDetectToChat() => coreCfg.Config.IsPrintWhenDetectToChat;
        internal static string GetChatMessage() => $" {coreCfg.Config.PluginPrefix} {coreCfg.Config.ChatMessage}";
        internal static string GetAdminPrefix() => coreCfg.Config.PluginPrefix;
        internal static string GetKickCommand(PlayerData player)
        {
            var command = coreCfg.Config.KickCommand;
            return ReplacePlaceholders(command, player, null);
        }

        internal static string GetBanCommand(PlayerData player, string reason)
        {
            var command = coreCfg.Config.BanCommand;
            return ReplacePlaceholders(command, player, reason);
        }

        private static string ReplacePlaceholders(string command, PlayerData player, string? reason)
        {
            return command
                .Replace("{player_steamid}", player.Controller.AuthorizedSteamID!.SteamId64.ToString())
                .Replace("{player_ingameid}", player.Controller.Index.ToString())
                .Replace("{player_nickname}", player.Controller.PlayerName)
                .Replace("{reason}", reason ?? string.Empty);
        }
        public class CoreCfgSaveData
        {
            public string BanReason { get; set; } = "Использование стороннего ПО";
            public bool IsPrintCheatToReason { get; set; } = true;
            public bool IsPrintWhenDetectToChat { get; set; } = true;
            public string PluginPrefix { get; set; } = "{lime}[TBAC]{default} ";
            public string ChatMessage { get; set; } = "У игрока {red}{nickname} обнаружен {lightpurple}{reason}";
            public string BanCommand { get; set; } = "css_ban #{player_steamid} 0 {reason}";
            public string KickCommand { get; set; } = "css_kick #{player_steamid} {reason}";
        }
    } 
}
