using GameNetcodeStuff;
using HarmonyLib;
using static Something.Plugin;

namespace Something
{
    [HarmonyPatch]
    public class Patches
    {
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
            Plugin.onShipLanded.Invoke();
        }
    }
}
