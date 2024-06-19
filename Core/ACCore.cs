using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using TBAntiCheat.Handlers;
using CounterStrikeSharp.API.Core.Capabilities;
using IksAdminApi;
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
        public override string ModuleVersion => "0.5.0";
        public override string ModuleAuthor => "SemKrill & Killer_bigpoint";
        public override string ModuleDescription => "Proper Anti-Cheat for CS2";

        private static ACCore core;
        private static ILogger? logger = null;
        private readonly PluginCapability<IIksAdminApi> _pluginCapability = new("iksadmin:core");
        public static IIksAdminApi? AdminApi;
        private static BaseConfig<CoreCfgSaveData> coreCfg;

        public override void Load(bool hotReload)
        {
            core = this;
            logger = Logger;

            CommandHandler.InitializeCommandHandler(this);

            EventListeners.InitializeListeners(this);
            EventHandlers.InitializeHandlers(this);

            coreCfg = new BaseConfig<CoreCfgSaveData>("Core");

            Log($"[TBAC] Loaded (v{ModuleVersion})");
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            AdminApi = _pluginCapability.Get();
        }

        internal static void Log(string message)
        {
            if (logger == null)
            {
                return;
            }

            logger.Log(LogLevel.Information, message);
        }

        internal static ACCore GetCore()
        {
            return core;
        }

        internal static string GetBanReason()
        {
            return coreCfg.Config.BanReason;
        }

        internal static bool GetIsPrintToReasinCheatInfo()
        {
            return coreCfg.Config.IsPrintCheatToReason;
        }

        internal static bool GetIsPrintDetectToChat()
        {
            return coreCfg.Config.IsPrintWhenDetectToChat;
        }

        internal static string GetChatMessage()
        {
            return $" {coreCfg.Config.PluginPrefix} {coreCfg.Config.ChatMessage}";
        }

        internal static string GetAdminPrefix()
        {
            return coreCfg.Config.PluginPrefix;
        }

        public class CoreCfgSaveData
        {
            public string BanReason { get; set; } = "Использование стороннего ПО";
            public bool IsPrintCheatToReason { get; set; } = true;
            public bool IsPrintWhenDetectToChat { get; set; } = true;
            public string PluginPrefix { get; set; } = "{lime}[TBAC]{default} ";
            public string ChatMessage { get; set; } = "У игрока {red}{nickname} обнаружен {lightpurple}{reason}";
        }
    } 
}
