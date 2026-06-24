using HarmonyLib;
using SnowyLib;
using Something.Enemies;
using UnityEngine;
using static Something.Plugin;

namespace Something
{
    [HarmonyPatch]
    public class TESTING : MonoBehaviour
    {
        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            try
            {
                if (!Utils.testing) { return; }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                if (!Utils.testing) { return; }
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");
                logger.LogDebug(msg);

                switch (args[0])
                {
                    case "/index":
                        SpringCatAI.SpringCatKillIndex = int.Parse(args[1]);
                        HUDManager.Instance.DisplayTip("SpringCatKillIndex", SpringCatAI.SpringCatKillIndex.ToString());
                        break;
                    default:
                        break;
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}