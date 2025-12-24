using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Something.Items.Polaroids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Events;
using static Something.Plugin;

namespace Something
{
    [HarmonyPatch]
    public class Patches
    {
        public static UnityEvent onShipLanded = new UnityEvent();

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        public static void OnShipLandedMiscEventsPostfix()
        {
            onShipLanded.Invoke();
        }
    }
}
