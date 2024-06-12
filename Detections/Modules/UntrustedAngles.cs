﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using TBAntiCheat.Core;
using TBAntiCheat.Handlers;

namespace TBAntiCheat.Detections.Modules
{
    public class UntrustedAnglesSaveData
    {
        public bool DetectionEnabled { get; set; } = true;
        public ActionType DetectionAction { get; set; } = ActionType.Kick;
    }

    /*
     * Module: Eye Angles
     * Purpose: Detect players which use eye angles that are outside the normal limit.
     * NOTE: Is this even needed anymore in CS2?
     */
    internal class UntrustedAngles : BaseDetection
    {
        internal override string Name => "UntrustedAngles";
        internal override ActionType ActionType => config.Config.DetectionAction;

        private readonly BaseConfig<UntrustedAnglesSaveData> config;

        internal UntrustedAngles() : base()
        {
            config = new BaseConfig<UntrustedAnglesSaveData>("UntrustedAngles");

            CommandHandler.RegisterCommand("tbac_untrustedangles_enable", "Deactivates/Activates UntrustedAngles detections", OnEnableCommand);
            CommandHandler.RegisterCommand("tbac_untrustedangles_action", "Which action to take on the player. 0 = none | 1 = log | 2 = kick | 3 = ban", OnActionCommand);
        }

        internal override void OnPlayerTick(PlayerData player)
        {
            if (config.Config.DetectionEnabled == false)
            {
                return;
            }

            QAngle eyeAngles = player.Pawn.EyeAngles;
            float x = eyeAngles.X;
            float y = eyeAngles.Y;
            float z = eyeAngles.Z;

            //Normal eye angles. Anything outside of this is untrusted
            if (x >= -89f && x <= 89f &&
                y >= -180f && y <= 180f &&
                z >= -50f && z <= 50f)
            {
                return;
            }

            string reason = $"Untrusted EyeAngles -> {x} {y} {z}";
            OnPlayerDetected(player, reason);
        }

        // ----- Commands ----- \\

        [RequiresPermissions("@css/admin")]
        private void OnEnableCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount != 2)
            {
                return;
            }

            string arg = command.ArgByIndex(1);
            if (bool.TryParse(arg, out bool state) == false)
            {
                return;
            }

            config.Config.DetectionEnabled = state;
            config.Save();
        }

        [RequiresPermissions("@css/admin")]
        private void OnActionCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (command.ArgCount != 2)
            {
                return;
            }

            string arg = command.ArgByIndex(1);
            if (int.TryParse(arg, out int action) == false)
            {
                return;
            }

            ActionType actionType = (ActionType)action;
            if (config.Config.DetectionAction.HasFlag(actionType) == false)
            {
                return;
            }

            config.Config.DetectionAction = actionType;
            config.Save();
        }
    }
}