using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Something
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    [BepInDependency(LethalCompanyInputUtils.PluginInfo.PLUGIN_GUID)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return GameNetworkManager.Instance.localPlayerController; } }
        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts.Where(x => x.actualClientId == id).First(); }
        public static bool IsServerOrHost { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }

        public static AssetBundle? ModAssets;

        // Configs
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        // Debugging
        public static ConfigEntry<bool> configEnableDebugging;

        // Something Configs
        //public static ConfigEntry<bool> configEnableSomething;
        public static ConfigEntry<string> configSomethingLevelRarities;
        public static ConfigEntry<string> configSomethingCustomLevelRarities;

        // Polaroid Configs

        /*public static ConfigEntry<bool> configEnablePolaroids;
        public static ConfigEntry<string> configGoodPolaroidLevelRarities;
        public static ConfigEntry<string> configGoodPolaroidCustomLevelRarities;
        public static ConfigEntry<int> configGoodPolaroidMinValue;
        public static ConfigEntry<int> configGoodPolaroidMaxValue;

        public static ConfigEntry<string> configNeutralPolaroidLevelRarities;
        public static ConfigEntry<string> configNeutralPolaroidCustomLevelRarities;
        public static ConfigEntry<int> configNeutralPolaroidMinValue;
        public static ConfigEntry<int> configNeutralPolaroidMaxValue;

        public static ConfigEntry<string> configBadPolaroidLevelRarities;
        public static ConfigEntry<string> configBadPolaroidCustomLevelRarities;
        public static ConfigEntry<int> configBadPolaroidMinValue;
        public static ConfigEntry<int> configBadPolaroidMaxValue;

        public static ConfigEntry<string> configCursedPolaroidLevelRarities;
        public static ConfigEntry<string> configCursedPolaroidCustomLevelRarities;
        public static ConfigEntry<int> configCursedPolaroidMinValue;
        public static ConfigEntry<int> configCursedPolaroidMaxValue;*/
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;

            harmony.PatchAll();

            InitializeNetworkBehaviours();

            SomethingInputs.Init();

            // Configs

            // Debugging Configs
            configEnableDebugging = Config.Bind("Debuggin", "Enable Debugging", false, "Set to true to enable debug logs");

            // SCP-4666 Configs
            //configEnableSomething = Config.Bind("Something", "Enable Something", true, "Set to false to disable spawning Something.");
            configSomethingLevelRarities = Config.Bind("Something Rarities", "Level Rarities", "All: 20", "Rarities for each level. See default for formatting.");
            configSomethingCustomLevelRarities = Config.Bind("Something Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");

            // Polaroid Configs
            /*configEnablePolaroids = Config.Bind("Polaroid Rarities", "Enable Polaroids", false, "Enables polaroids from the game as scrap.");

            configGoodPolaroidLevelRarities = Config.Bind("Polaroid Rarities", "Good Polaroid Level Rarities", "All: 10", "Rarities for Good Polaroids.");
            configGoodPolaroidCustomLevelRarities = Config.Bind("Polaroid Rarities", "Good Polaroid Custom Level Rarities", "", "Custom rarities for Good Polaroids.");

            configGoodPolaroidMinValue = Config.Bind("Polaroids", "Good Polaroid Min Value", 5, "Minimum value for Good Polaroid.");
            configGoodPolaroidMaxValue = Config.Bind("Polaroids", "Good Polaroid Max Value", 50, "Maximum value for Good Polaroid.");


            configNeutralPolaroidLevelRarities = Config.Bind("Polaroid Rarities", "Neutral Polaroid Level Rarities", "All: 20", "Rarities for Neutral Polaroids.");
            configNeutralPolaroidCustomLevelRarities = Config.Bind("Polaroid Rarities", "Neutral Polaroid Custom Level Rarities", "", "Custom rarities for Neutral Polaroids.");

            configNeutralPolaroidMinValue = Config.Bind("Polaroids", "Neutral Polaroid Min Value", 5, "Minimum value for Neutral Polaroid.");
            configNeutralPolaroidMaxValue = Config.Bind("Polaroids", "Neutral Polaroid Max Value", 50, "Maximum value for Neutral Polaroid.");


            configBadPolaroidLevelRarities = Config.Bind("Polaroid Rarities", "Bad Polaroid Level Rarities", "All: 15", "Rarities for Bad Polaroids.");
            configBadPolaroidCustomLevelRarities = Config.Bind("Polaroid Rarities", "Bad Polaroid Custom Level Rarities", "", "Custom rarities for Bad Polaroids.");

            configBadPolaroidMinValue = Config.Bind("Polaroids", "Bad Polaroid Min Value", 10, "Minimum value for Bad Polaroid.");
            configBadPolaroidMaxValue = Config.Bind("Polaroids", "Bad Polaroid Max Value", 100, "Maximum value for Bad Polaroid.");


            configCursedPolaroidLevelRarities = Config.Bind("Polaroid Rarities", "Cursed Polaroid Level Rarities", "All: 5", "Rarities for Cursed Polaroids.");
            configCursedPolaroidCustomLevelRarities = Config.Bind("Polaroid Rarities", "Cursed Polaroid Custom Level Rarities", "", "Custom rarities for Cursed Polaroids.");

            configCursedPolaroidMinValue = Config.Bind("Polaroids", "Cursed Polaroid Min Value", 10, "Minimum value for Cursed Polaroid.");
            configCursedPolaroidMaxValue = Config.Bind("Polaroids", "Cursed Polaroid Max Value", 100, "Maximum value for Cursed Polaroid.");*/


            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "something_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "something_assets")}");

            // Good Polaroid
            /*Item GoodPolaroid = ModAssets.LoadAsset<Item>("Assets/ModAssets/GoodPolaroidItem.asset");
            if (GoodPolaroid == null) { LoggerInstance.LogError("Error: Couldnt get GoodPolaroidItem from assets"); return; }
            LoggerInstance.LogDebug($"Got GoodPolaroid prefab");

            GoodPolaroid.minValue = configGoodPolaroidMinValue.Value;
            GoodPolaroid.maxValue = configGoodPolaroidMaxValue.Value;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(GoodPolaroid.spawnPrefab);
            Utilities.FixMixerGroups(GoodPolaroid.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(GoodPolaroid, GetLevelRarities(configGoodPolaroidLevelRarities.Value), GetCustomLevelRarities(configGoodPolaroidCustomLevelRarities.Value));

            // Neutral Polaroid
            Item NeutralPolaroid = ModAssets.LoadAsset<Item>("Assets/ModAssets/NeutralPolaroidItem.asset");
            if (NeutralPolaroid == null) { LoggerInstance.LogError("Error: Couldnt get NeutralPolaroidItem from assets"); return; }
            LoggerInstance.LogDebug($"Got NeutralPolaroid prefab");

            NeutralPolaroid.minValue = configNeutralPolaroidMinValue.Value;
            NeutralPolaroid.maxValue = configNeutralPolaroidMaxValue.Value;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(NeutralPolaroid.spawnPrefab);
            Utilities.FixMixerGroups(NeutralPolaroid.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(NeutralPolaroid, GetLevelRarities(configNeutralPolaroidLevelRarities.Value), GetCustomLevelRarities(configNeutralPolaroidCustomLevelRarities.Value));

            // Bad Polaroid
            Item BadPolaroid = ModAssets.LoadAsset<Item>("Assets/ModAssets/BadPolaroidItem.asset");
            if (BadPolaroid == null) { LoggerInstance.LogError("Error: Couldnt get BadPolaroidItem from assets"); return; }
            LoggerInstance.LogDebug($"Got BadPolaroid prefab");

            BadPolaroid.minValue = configBadPolaroidMinValue.Value;
            BadPolaroid.maxValue = configBadPolaroidMaxValue.Value;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(BadPolaroid.spawnPrefab);
            Utilities.FixMixerGroups(BadPolaroid.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(BadPolaroid, GetLevelRarities(configBadPolaroidLevelRarities.Value), GetCustomLevelRarities(configBadPolaroidCustomLevelRarities.Value));

            // Cursed Polaroid
            Item CursedPolaroid = ModAssets.LoadAsset<Item>("Assets/ModAssets/CursedPolaroidItem.asset");
            if (CursedPolaroid == null) { LoggerInstance.LogError("Error: Couldnt get CursedPolaroidItem from assets"); return; }
            LoggerInstance.LogDebug($"Got CursedPolaroid prefab");

            CursedPolaroid.minValue = configCursedPolaroidMinValue.Value;
            CursedPolaroid.maxValue = configCursedPolaroidMaxValue.Value;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(CursedPolaroid.spawnPrefab);
            Utilities.FixMixerGroups(CursedPolaroid.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(CursedPolaroid, GetLevelRarities(configCursedPolaroidLevelRarities.Value), GetCustomLevelRarities(configCursedPolaroidCustomLevelRarities.Value));*/

            /*if (configEnableSomething.Value)
            {
            }*/
            EnemyType something = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/SomethingEnemy.asset");
            if (something == null) { LoggerInstance.LogError("Error: Couldnt get Something enemy from assets"); return; }
            LoggerInstance.LogDebug($"Got Something enemy prefab");
            TerminalNode SomethingTN = ModAssets.LoadAsset<TerminalNode>("Assets/ModAssets/Bestiary/SomethingTN.asset");
            TerminalKeyword SomethingTK = ModAssets.LoadAsset<TerminalKeyword>("Assets/ModAssets/Bestiary/SomethingTK.asset");

            LoggerInstance.LogDebug("Registering enemy network prefab...");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(something.enemyPrefab);
            LoggerInstance.LogDebug("Registering enemy...");
            Enemies.RegisterEnemy(something, GetLevelRarities(configSomethingLevelRarities.Value), GetCustomLevelRarities(configSomethingCustomLevelRarities.Value), SomethingTN, SomethingTK);

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        public Dictionary<Levels.LevelTypes, int> GetLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<Levels.LevelTypes, int> levelRaritiesDict = new Dictionary<Levels.LevelTypes, int>();

                if (levelsString != null && levelsString != "")
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (Enum.TryParse<Levels.LevelTypes>(levelType, out Levels.LevelTypes levelTypeEnum) && int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            levelRaritiesDict.Add(levelTypeEnum, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return levelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null!;
            }
        }

        public Dictionary<string, int> GetCustomLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<string, int> customLevelRaritiesDict = new Dictionary<string, int>();

                if (levelsString != null)
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            customLevelRaritiesDict.Add(levelType, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return customLevelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null!;
            }
        }

        public static void FreezePlayer(PlayerControllerB player, bool value)
        {
            player.disableInteract = value;
            player.disableLookInput = value;
            player.disableMoveInput = value;
        }

        public static void log(string msg)
        {
            if (configEnableDebugging.Value)
            {
                LoggerInstance.LogDebug(msg);
            }
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
            LoggerInstance.LogDebug("Finished initializing network behaviours");
        }
    }
}
