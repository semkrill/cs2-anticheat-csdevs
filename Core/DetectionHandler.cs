using IksAdminApi;
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

                    var playerInfo = new PlayerInfo(
                        metadata.player.Controller.PlayerName,
                        metadata.player.Controller.AuthorizedSteamID!.SteamId64,
                        metadata.player.Controller.IpAddress!.Split(":")[0]
                    );

                    ACCore.AdminApi.EKick("CONSOLE", playerInfo, $"{metadata.detection.Name} ({metadata.reason})");
                    SendChatMessage(metadata);
                    break;

                case ActionType.Ban:
                    ACCore.Log($"[TBAC] {metadata.player.Controller.PlayerName} was banned for using {metadata.detection.Name} ({metadata.reason})");

                    string reasonForBan = ACCore.GetBanReason();
                    if (ACCore.GetIsPrintToReasinCheatInfo())
                    {
                        reasonForBan = $"{reasonForBan} ({metadata.reason})";
                    }

                    var playerBan = new PlayerBan(
                        name: metadata.player.Controller.PlayerName,
                        sid: metadata.player.Controller.AuthorizedSteamID!.SteamId64.ToString(),
                        ip: metadata.player.Controller.IpAddress!.Split(":")[0],
                        adminSid: "CONSOLE",
                        adminName: "Console",
                        created: GetUnixTimeSeconds(),
                        time: 0,
                        end: 0,
                        reason: reasonForBan,
                        serverId: ACCore.AdminApi.Config.ServerId,
                        banType: 0,
                        unbanned: 0,
                        unbannedBy: null,
                        id: null
                    );

                    bool result = await ACCore.AdminApi.AddBan("CONSOLE", playerBan);

                    if (result)
                    {
                        var playerInfo2 = new PlayerInfo(
                            metadata.player.Controller.PlayerName,
                            metadata.player.Controller.AuthorizedSteamID!.SteamId64,
                            metadata.player.Controller.IpAddress!.Split(":")[0]
                        );

                        ACCore.AdminApi.EKick("CONSOLE", playerInfo2, $"{metadata.detection.Name} ({metadata.reason})");
                        SendChatMessage(metadata);
                    }
                    else
                    {
                        Console.WriteLine("Failed to ban the player.");
                    }
                    break;
            }
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
