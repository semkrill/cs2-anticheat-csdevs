using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Security.Cryptography;
using TBAntiCheat.Core;
using TBAntiCheat.Handlers;

namespace TBAntiCheat.Detections.Modules
{
    public class AimbotSaveData
    {
        public bool DetectionEnabled { get; set; } = true;
        public ActionType DetectionAction { get; set; } = ActionType.Kick;

        public float MaxAimbotAngle { get; set; } = 30f;
        public int MaxDetectionsBeforeAction { get; set; } = 2;

        public float MaxHeadshotAngle { get; set; } = 15f;
        public float MaxBodyshotAngle { get; set; } = 20f;
        public int MaxHeadshotDetectionsBeforeAction { get; set; } = 1;
        public int MaxBodyshotDetectionsBeforeAction { get; set; } = 2;
    }

    internal struct AngleSnapshot
    {
        internal float x;
        internal float y;
        internal float z;

        public AngleSnapshot()
        {
            Reset();
        }

        public AngleSnapshot(Vector? vector)
        {
            if (vector == null)
            {
                Reset();
                return;
            }

            x = vector.X;
            y = vector.Y;
            z = vector.Z;
        }

        public AngleSnapshot(QAngle? angle)
        {
            if (angle == null)
            {
                Reset();
                return;
            }

            x = angle.X;
            y = angle.Y;
            z = angle.Z;
        }

        public static float Distance(AngleSnapshot a, AngleSnapshot b)
        {
            float deltaX = a.x - b.x;
            float deltaY = a.y - b.y;
            float deltaZ = a.z - b.z;

            return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        internal void Reset()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public override readonly string ToString()
        {
            return $"{x:n2} {y:n2} {z:n2}";
        }
    }

    internal class PlayerAimbotData
    {
        internal static readonly int aimbotMaxHistory = 64; // 1 entire second worth of history (considering the tickrate is 64)
        internal AngleSnapshot[] eyeAngleHistory;
        internal int historyIndex;
        internal int headshotDetections;
        internal int bodyshotDetections;

        public PlayerAimbotData()
        {
            eyeAngleHistory = new AngleSnapshot[aimbotMaxHistory];
            historyIndex = 0;
            headshotDetections = 0;
            bodyshotDetections = 0;
        }

        internal void Reset()
        {
            historyIndex = 0;
            headshotDetections = 0;
            bodyshotDetections = 0;

            for (int i = 0; i < eyeAngleHistory.Length; i++)
            {
                eyeAngleHistory[i].Reset();
            }
        }
    }

    internal class Aimbot : BaseDetection
    {
        private readonly BaseConfig<AimbotSaveData> config;
        private readonly PlayerAimbotData[] playerAimbotData;

        internal Aimbot() : base()
        {
            config = new BaseConfig<AimbotSaveData>("Aimbot");
            playerAimbotData = new PlayerAimbotData[Server.MaxPlayers];

            for (int i = 0; i < playerAimbotData.Length; i++)
            {
                playerAimbotData[i] = new PlayerAimbotData();
            }

            CommandHandler.RegisterCommand("tbac_aimbot_enable", "Activates/Deactivates the aimbot detection", OnEnableCommand);
            CommandHandler.RegisterCommand("tbac_aimbot_action", "Which action to take on the player. 0 = none | 1 = log | 2 = kick | 3 = ban", OnActionCommand);
            CommandHandler.RegisterCommand("tbac_aimbot_angle", "Max angle in a single tick before detection", OnAngleCommand);
            CommandHandler.RegisterCommand("tbac_aimbot_detections", "Maximum detections before an action should be taken", OnDetectionsCommand);
        }

        internal override string Name => "Aimbot";
        internal override ActionType ActionType => config.Config.DetectionAction;

        internal override void OnPlayerJoin(PlayerData player)
        {
            playerAimbotData[player.Index].Reset();
        }

        internal override void OnPlayerDead(PlayerData victim, PlayerData shooter)
        {
            if (!config.Config.DetectionEnabled)
            {
                return;
            }

            if (victim.Pawn.AbsOrigin == null || shooter.Pawn.AbsOrigin == null)
            {
                return;
            }

            PlayerAimbotData aimbotData = playerAimbotData[shooter.Index];
            AngleSnapshot lastAngle = aimbotData.eyeAngleHistory[aimbotData.historyIndex];
            float maxHeadshotAngle = config.Config.MaxHeadshotAngle;
            float maxBodyshotAngle = config.Config.MaxBodyshotAngle;
            bool isHeadshot = victim.Pawn.LastHitGroup == HitGroup_t.HITGROUP_HEAD;

            for (int i = 0; i < PlayerAimbotData.aimbotMaxHistory; i++)
            {
                AngleSnapshot currentAngle = aimbotData.eyeAngleHistory[(aimbotData.historyIndex + i) % PlayerAimbotData.aimbotMaxHistory];
                float angleDiff = AngleSnapshot.Distance(lastAngle, currentAngle);

                // Normalize angle difference
                if (angleDiff > 180f)
                {
                    angleDiff = MathF.Abs(angleDiff - 360);
                }

                if ((isHeadshot && angleDiff > maxHeadshotAngle) || (!isHeadshot && angleDiff > maxBodyshotAngle))
                {
                    OnAimbotDetected(shooter, aimbotData, angleDiff, isHeadshot);
                    break;
                }

                lastAngle = currentAngle;
            }
        }

        internal override void OnPlayerTick(PlayerData player)
        {
            if (!config.Config.DetectionEnabled)
            {
                return;
            }

            PlayerAimbotData aimbotData = playerAimbotData[player.Index];
            aimbotData.eyeAngleHistory[aimbotData.historyIndex] = new AngleSnapshot(player.Pawn.EyeAngles);

            aimbotData.historyIndex = (aimbotData.historyIndex + 1) % PlayerAimbotData.aimbotMaxHistory;
        }

        internal override void OnRoundStart()
        {
            foreach (var player in Globals.Players.Values)
            {
                playerAimbotData[player.Index].Reset();
            }
        }

        private void OnAimbotDetected(PlayerData player, PlayerAimbotData data, float angleDiff, bool isHeadshot)
        {
            if (isHeadshot)
            {
                data.headshotDetections++;
                int maxDetections = config.Config.MaxHeadshotDetectionsBeforeAction;

                ACCore.Log($"[TBAC] {player.Controller.PlayerName}: Suspicious headshot aimbot -> {angleDiff} degrees ({data.headshotDetections}/{maxDetections} detections)");
                string reason = $"Headshot Aimbot -> {angleDiff}";

                DetectionMetadata metadata = new DetectionMetadata()
                {
                    detection = this,
                    player = player,
                    time = DateTime.Now,
                    reason = reason
                };

                DetectionHandler.SendChatMessage(metadata);

                if (data.headshotDetections >= maxDetections)
                {
                    OnPlayerDetected(player, reason);
                }
            }
            else
            {
                data.bodyshotDetections++;
                int maxDetections = config.Config.MaxBodyshotDetectionsBeforeAction;

                ACCore.Log($"[TBAC] {player.Controller.PlayerName}: Suspicious bodyshot aimbot -> {angleDiff} degrees ({data.bodyshotDetections}/{maxDetections} detections)");
                string reason = $"Bodyshot Aimbot -> {angleDiff}";

                DetectionMetadata metadata = new DetectionMetadata()
                {
                    detection = this,
                    player = player,
                    time = DateTime.Now,
                    reason = reason
                };

                DetectionHandler.SendChatMessage(metadata);

                if (data.bodyshotDetections >= maxDetections)
                {
                    OnPlayerDetected(player, reason);
                }
            }
        }

        // ----- Commands ----- \\

        [RequiresPermissions("@css/admin")]
        private void OnEnableCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount != 2)
            {
                return;
            }

            if (bool.TryParse(command.ArgByIndex(1), out bool state))
            {
                config.Config.DetectionEnabled = state;
                config.Save();
            }
        }

        [RequiresPermissions("@css/admin")]
        private void OnActionCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount != 2)
            {
                return;
            }

            if (int.TryParse(command.ArgByIndex(1), out int action))
            {
                config.Config.DetectionAction = (ActionType)action;
                config.Save();
            }
        }

        [RequiresPermissions("@css/admin")]
        private void OnAngleCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount != 2)
            {
                return;
            }

            if (float.TryParse(command.ArgByIndex(1), out float angle))
            {
                config.Config.MaxAimbotAngle = angle;
                config.Save();
            }
        }

        [RequiresPermissions("@css/admin")]
        private void OnDetectionsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount != 2)
            {
                return;
            }

            if (int.TryParse(command.ArgByIndex(1), out int detections))
            {
                config.Config.MaxDetectionsBeforeAction = detections;
                config.Save();
            }
        }
    }
}