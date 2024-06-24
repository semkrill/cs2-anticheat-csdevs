using TBAntiCheat.Detections;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API;

namespace TBAntiCheat.Core
{
    internal struct DetectionMetadata
    {
        internal BaseDetection detection;
        internal PlayerData player;
        internal DateTime time;
        internal string reason;
    }

    internal static class DetectionHandler
    {
        internal static async Task OnPlayerDetectedAsync(DetectionMetadata metadata)
        {
            switch (metadata.detection.ActionType)
            {
                case ActionType.None:
                    return;

                case ActionType.Log:
                    ACCore.Log($"[TBAC] {metadata.player.Controller.PlayerName} is suspected of using {metadata.detection.Name} ({metadata.reason})");
                    SendChatMessage(metadata);
                    break;

                case ActionType.Kick:
                    ACCore.Log($"[TBAC] {metadata.player.Controller.PlayerName} was kicked for using {metadata.detection.Name} ({metadata.reason})");

                    var kickCommand = ACCore.GetKickCommand(metadata.player);
                    ExecuteConsoleCommand(kickCommand);
                    SendChatMessage(metadata);
                    break;

                case ActionType.Ban:
                    ACCore.Log($"[TBAC] {metadata.player.Controller.PlayerName} was banned for using {metadata.detection.Name} ({metadata.reason})");

                    string reasonForBan = ACCore.GetBanReason();
                    if (ACCore.GetIsPrintToReasinCheatInfo())
                    {
                        reasonForBan = $"{reasonForBan} ({metadata.reason})";
                    }

                    var banCommand = ACCore.GetBanCommand(metadata.player, reasonForBan);
                    ExecuteConsoleCommand(banCommand);
                    SendChatMessage(metadata);
                    break;
            }
        }
        private static void ExecuteConsoleCommand(string command)
        {
            Server.ExecuteCommand(command);
        }
        private static int GetUnixTimeSeconds()
        {
            return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private static string GetFormattedChatMessage(string playerName, string reason)
        {
            string messageTemplate = ACCore.GetChatMessage();
            string message = messageTemplate.Replace("{nickname}", playerName).Replace("{reason}", reason);
            return ReplaceColors(message);
        }

        private static string ReplaceColors(string str)
        {
            return str
                .Replace("{default}", "\x01")
                .Replace("{white}", "\x01")
                .Replace("{darkred}", "\x02")
                .Replace("{green}", "\x04")
                .Replace("{lightyellow}", "\x09")
                .Replace("{lightblue}", "\x0B")
                .Replace("{olive}", "\x05")
                .Replace("{lime}", "\x06")
                .Replace("{red}", "\x07")
                .Replace("{lightpurple}", "\x03")
                .Replace("{purple}", "\x0E")
                .Replace("{grey}", "\x08")
                .Replace("{yellow}", "\x09")
                .Replace("{gold}", "\x10")
                .Replace("{silver}", "\x0A")
                .Replace("{blue}", "\x0B")
                .Replace("{darkblue}", "\x0C")
                .Replace("{bluegrey}", "\x0A")
                .Replace("{magenta}", "\x0E")
                .Replace("{lightred}", "\x0F")
                .Replace("{orange}", "\x10");
        }

        public static void SendChatMessage(DetectionMetadata metadata)
        {
            string formattedChatMessage = GetFormattedChatMessage(metadata.player.Controller.PlayerName, metadata.detection.Name);
            Server.PrintToChatAll(formattedChatMessage);
        }
    }
}
