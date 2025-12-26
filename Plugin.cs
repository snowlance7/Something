using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Dawn;
using Dawn.Utils;
using Dusk;
using GameNetcodeStuff;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace Something
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(DawnLib.PLUGIN_GUID)]
    [BepInDependency(LethalCompanyInputUtils.PluginInfo.PLUGIN_GUID)]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS8618
        public static Plugin PluginInstance;
        public static ManualLogSource logger { get; private set; }
        public static DuskMod Mod { get; private set; }

        public static ConfigEntry<bool> configMinimalSpoilerVersion;
        //public static ConfigEntry<float> configBadPolaroidSomethingChance;
#pragma warning restore CS8618

        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return GameNetworkManager.Instance.localPlayerController; } }
        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts.Where(x => x.actualClientId == id).First(); }
        public static bool IsServer { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }

        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            logger = PluginInstance.Logger;

            harmony.PatchAll();

            AssetBundle? mainBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "something_mainassets"));
            Mod = DuskMod.RegisterMod(this, mainBundle);
            Mod.RegisterContentHandlers();

            InitializeNetworkBehaviours();

            SomethingInputs.Init();

            InitConfigs();

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }
        /*public class DuckSongAssets(DuskMod mod, string filePath) : AssetBundleLoader<DuckSongAssets>(mod, filePath)
        {
            [LoadFromBundle("DuckHolder.prefab")]
            public GameObject DuckUIPrefab { get; private set; } = null!;
        }*/

        void InitConfigs()
        {
            // General
            configMinimalSpoilerVersion = Config.Bind("Spoilers", "Minimal Spoiler Version", true, "Replaces most spoilers for the game with alternatives.");
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

        private static void InitializeNetworkBehaviours()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            logger.LogDebug("Finished initializing network behaviours");
        }
    }
}
