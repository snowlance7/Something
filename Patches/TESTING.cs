using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

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
    internal class TESTING : MonoBehaviour
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;
        private static bool toggle;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {

        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");

                switch (args[0])
                {
                    case "/refresh":
                        RoundManager.Instance.RefreshEnemiesList();
                        HoarderBugAI.RefreshGrabbableObjectsInMapList();
                        break;
                    case "/levels":
                        foreach (var level in StartOfRound.Instance.levels)
                        {
                            logger.LogDebug(level.name);
                        }
                        break;
                    case "/dungeon":
                        logger.LogDebug(RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name);
                        break;
                    case "/dungeons":
                        foreach (var dungeon in RoundManager.Instance.dungeonFlowTypes)
                        {
                            logger.LogDebug(dungeon.dungeonFlow.name);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                logger.LogError("Invalid chat command");
                return;
            }
        }
    }
}