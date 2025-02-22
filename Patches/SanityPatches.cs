using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Something.Patches
{
    [HarmonyPatch]
    internal class SanityPatches
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.NearOtherPlayers))]
        public static bool NearOtherPlayersPrefix(PlayerControllerB __instance, ref bool __result)
        {
            try
            {
                if (IsPlayerHaunted(__instance))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsPlayerHaunted(PlayerControllerB player)
        {
            if (StartOfRound.Instance.inShipPhase) { return false; }
            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemy is SomethingAI && enemy.targetPlayer == player)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
