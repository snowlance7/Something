using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using static Something.Plugin;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace Something
{
    [HarmonyPatch]
    public class TESTING : MonoBehaviour
    {
        private static ManualLogSource logger = LoggerInstance;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.testing) { return; }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            string msg = __instance.chatTextField.text;
            string[] args = msg.Split(" ");
            logger.LogDebug(msg);

            switch (args[0])
            {
                case "/killIndex":
                    SpringCatAI.SpringCatKillIndex = int.Parse(args[1]);
                    HUDManager.Instance.DisplayTip("SpringCatKillIndex", SpringCatAI.SpringCatKillIndex.ToString());
                    break;
                case "/targetting":
                    Utils.DEBUG_disableTargetting = !Utils.DEBUG_disableTargetting;
                    HUDManager.Instance.DisplayTip("DisableTargetting: ", Utils.DEBUG_disableTargetting.ToString());
                    break;
                default:
                    Utils.ChatCommand(args);
                    break;
            }
        }
    }
}