﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using TBAntiCheat.Detections;
using TBAntiCheat.Detections.Modules;

namespace TBAntiCheat.Core
{
    internal class PlayerData
    {
        internal required CCSPlayerController Controller;
        internal required CCSPlayerPawn Pawn;

        internal required int Index;
    }

    internal static class Globals
    {
        private static bool InitializedOnce = false;

        private static ACCore? pluginCore = null;

        internal static Dictionary<uint, PlayerData> Players = [];
        internal static BaseDetection[] Detections = [];

        internal static void PreInit(ACCore core)
        {
            pluginCore = core;
        }

        internal static void Initialize(bool hotReload)
        {
            if (InitializedOnce == true && hotReload == false)
            {
                return;
            }

            Players = new Dictionary<uint, PlayerData>(Server.MaxPlayers);
            Detections =
            [
                new Aimbot()
            ];

            InitializedOnce = true;
        }

        internal static string GetModuleDirectory()
        {
            if (pluginCore == null)
            {
                return string.Empty;
            }

            return pluginCore.ModuleDirectory;
        }
    }
}