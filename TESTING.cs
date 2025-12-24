using BepInEx.Logging;
using HarmonyLib;
using System.Linq;
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
        private static ManualLogSource logger = Plugin.logger;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.testing) { return; }

            /*for (int i = 0; i < playerListSlots.Length; i++) // playerListSlots is in QuickMenuManager
            {
                if (playerListSlots[i].isConnected)
                {
                    float num = playerListSlots[i].volumeSlider.value / playerListSlots[i].volumeSlider.maxValue;
                    if (num == -1f)
                    {
                        SoundManager.Instance.playerVoiceVolumes[i] = -70f;
                    }
                    else
                    {
                        SoundManager.Instance.playerVoiceVolumes[i] = num;
                    }
                }
            }*/
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            string msg = __instance.chatTextField.text;
            string[] args = msg.Split(" ");
            logger.LogDebug(msg);

            switch (args[0])
            {
                case "/index":
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