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

        // General
        public static ConfigEntry<bool> configSpoilerFreeVersion;

        // BreathingUI
        public static ConfigEntry<bool> configShowBreathingTooltip;

        // Something Configs
        public static ConfigEntry<string> configSomethingLevelRarities;
        public static ConfigEntry<string> configSomethingCustomLevelRarities;

        // Polaroid Configs

        public static ConfigEntry<bool> configEnablePolaroids;
        public static ConfigEntry<string> configGoodPolaroidLevelRarities;
        public static ConfigEntry<string> configGoodPolaroidCustomLevelRarities;
        public static ConfigEntry<int> configGoodPolaroidMinValue;
        public static ConfigEntry<int> configGoodPolaroidMaxValue;

        public static ConfigEntry<string> configBadPolaroidLevelRarities;
        public static ConfigEntry<string> configBadPolaroidCustomLevelRarities;
        public static ConfigEntry<int> configBadPolaroidMinValue;
        public static ConfigEntry<int> configBadPolaroidMaxValue;
        public static ConfigEntry<float> configBadPolaroidSomethingChance;
        public static ConfigEntry<string> configCursedPolaroidLevelRarities;
        public static ConfigEntry<string> configCursedPolaroidCustomLevelRarities;
        public static ConfigEntry<int> configCursedPolaroidMinValue;
        public static ConfigEntry<int> configCursedPolaroidMaxValue;
        public static ConfigEntry<float> configCursedPolaroidSomethingChance;

        // Config entries for Keytar
        public static ConfigEntry<string> configKeytarLevelRarities;
        public static ConfigEntry<string> configKeytarCustomLevelRarities;
        public static ConfigEntry<int> configKeytarMinValue;
        public static ConfigEntry<int> configKeytarMaxValue;

        // Config entries for AubreyPlush
        public static ConfigEntry<string> configAubreyPlushLevelRarities;
        public static ConfigEntry<string> configAubreyPlushCustomLevelRarities;
        public static ConfigEntry<int> configAubreyPlushMinValue;
        public static ConfigEntry<int> configAubreyPlushMaxValue;

        // Config entries for BasilPlush
        public static ConfigEntry<string> configBasilPlushLevelRarities;
        public static ConfigEntry<string> configBasilPlushCustomLevelRarities;
        public static ConfigEntry<int> configBasilPlushMinValue;
        public static ConfigEntry<int> configBasilPlushMaxValue;

        // Config entries for Bunnybun
        public static ConfigEntry<string> configBunnybunLevelRarities;
        public static ConfigEntry<string> configBunnybunCustomLevelRarities;
        public static ConfigEntry<int> configBunnybunMinValue;
        public static ConfigEntry<int> configBunnybunMaxValue;

        // Config entries for Mailbox
        public static ConfigEntry<string> configMailboxLevelRarities;
        public static ConfigEntry<string> configMailboxCustomLevelRarities;
        public static ConfigEntry<int> configMailboxMinValue;
        public static ConfigEntry<int> configMailboxMaxValue;
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

            // General
            configSpoilerFreeVersion = Config.Bind("Spoilers", "Spoiler-Free Version", true, "Replaces most spoilers for the game with alternatives.");

            // BreathingUI
            configShowBreathingTooltip = Config.Bind("BreathingUI", "Show Breathing Tooltip", true, "Shows the breathing tooltip on the top right when you are being haunted by Something.");

            // Something Configs
            configSomethingLevelRarities = Config.Bind("Rarities", "Something Level Rarities", "All: 40, Modded: 40", "Rarities for each level. See default for formatting.");
            configSomethingCustomLevelRarities = Config.Bind("Rarities", "Something Custom Level Rarities", "", "Rarities for modded levels.");

            // Polaroid Configs
            configEnablePolaroids = Config.Bind("Polaroid Rarities", "Enable Polaroids", true, "Enables polaroids from the game as scrap.");

            configGoodPolaroidLevelRarities = Config.Bind("Polaroid Rarities", "Good Polaroid Level Rarities", "All: 50, Modded: 50", "Rarities for Good Polaroids.");
            configGoodPolaroidCustomLevelRarities = Config.Bind("Polaroid Rarities", "Good Polaroid Custom Level Rarities", "", "Custom rarities for Good Polaroids.");
            configGoodPolaroidMinValue = Config.Bind("Polaroids", "Good Polaroid Min Value", 50, "Minimum value for Good Polaroid.");
            configGoodPolaroidMaxValue = Config.Bind("Polaroids", "Good Polaroid Max Value", 75, "Maximum value for Good Polaroid.");


            configBadPolaroidLevelRarities = Config.Bind("Polaroid Rarities", "Bad Polaroid Level Rarities", "All: 50, Modded: 50", "Rarities for Bad Polaroids.");
            configBadPolaroidCustomLevelRarities = Config.Bind("Polaroid Rarities", "Bad Polaroid Custom Level Rarities", "", "Custom rarities for Bad Polaroids.");
            configBadPolaroidMinValue = Config.Bind("Polaroids", "Bad Polaroid Min Value", 50, "Minimum value for Bad Polaroid.");
            configBadPolaroidMaxValue = Config.Bind("Polaroids", "Bad Polaroid Max Value", 100, "Maximum value for Bad Polaroid.");
            configBadPolaroidSomethingChance = Config.Bind("Polaroids", "Bad Polaroid Something Spawn Chance", 0.5f, "Chance of spawning somethiing on first player who picked up Bad Polaroid.");


            configCursedPolaroidLevelRarities = Config.Bind("Polaroid Rarities", "Cursed Polaroid Level Rarities", "All: 5, Modded: 5", "Rarities for Cursed Polaroids.");
            configCursedPolaroidCustomLevelRarities = Config.Bind("Polaroid Rarities", "Cursed Polaroid Custom Level Rarities", "", "Custom rarities for Cursed Polaroids.");
            configCursedPolaroidMinValue = Config.Bind("Polaroids", "Cursed Polaroid Min Value", 300, "Minimum value for Cursed Polaroid.");
            configCursedPolaroidMaxValue = Config.Bind("Polaroids", "Cursed Polaroid Max Value", 750, "Maximum value for Cursed Polaroid.");
            configCursedPolaroidSomethingChance = Config.Bind("Polaroids", "Cursed Polaroid Something Spawn Chance", 0.99f, "Chance of spawning somethiing on first player who picked up Cursed Polaroid.");

            // Keytar
            configKeytarLevelRarities = Config.Bind("Keytar Rarities", "Keytar Level Rarities", "All: 10, Modded: 10", "Rarities for Keytar.");
            configKeytarCustomLevelRarities = Config.Bind("Keytar Rarities", "Keytar Custom Level Rarities", "", "Custom rarities for Keytar.");
            configKeytarMinValue = Config.Bind("Keytar", "Keytar Min Value", 10, "Minimum value for Keytar.");
            configKeytarMaxValue = Config.Bind("Keytar", "Keytar Max Value", 100, "Maximum value for Keytar.");

            // AubreyPlush
            configAubreyPlushLevelRarities = Config.Bind("AubreyPlush Rarities", "AubreyPlush Level Rarities", "All: 20, Modded: 20", "Rarities for AubreyPlush.");
            configAubreyPlushCustomLevelRarities = Config.Bind("AubreyPlush Rarities", "AubreyPlush Custom Level Rarities", "", "Custom rarities for AubreyPlush.");
            configAubreyPlushMinValue = Config.Bind("AubreyPlush", "AubreyPlush Min Value", 5, "Minimum value for AubreyPlush.");
            configAubreyPlushMaxValue = Config.Bind("AubreyPlush", "AubreyPlush Max Value", 50, "Maximum value for AubreyPlush.");

            // BasilPlush
            configBasilPlushLevelRarities = Config.Bind("BasilPlush Rarities", "BasilPlush Level Rarities", "All: 20, Modded: 20", "Rarities for BasilPlush.");
            configBasilPlushCustomLevelRarities = Config.Bind("BasilPlush Rarities", "BasilPlush Custom Level Rarities", "", "Custom rarities for BasilPlush.");
            configBasilPlushMinValue = Config.Bind("BasilPlush", "BasilPlush Min Value", 5, "Minimum value for BasilPlush.");
            configBasilPlushMaxValue = Config.Bind("BasilPlush", "BasilPlush Max Value", 50, "Maximum value for BasilPlush.");

            // Bunnybun
            configBunnybunLevelRarities = Config.Bind("Bunnybun Rarities", "Bunnybun Level Rarities", "All: 20, Modded: 20", "Rarities for Bunnybun.");
            configBunnybunCustomLevelRarities = Config.Bind("Bunnybun Rarities", "Bunnybun Custom Level Rarities", "", "Custom rarities for Bunnybun.");
            configBunnybunMinValue = Config.Bind("Bunnybun", "Bunnybun Min Value", 5, "Minimum value for Bunnybun.");
            configBunnybunMaxValue = Config.Bind("Bunnybun", "Bunnybun Max Value", 50, "Maximum value for Bunnybun.");

            // Mailbox
            configMailboxLevelRarities = Config.Bind("Mailbox Rarities", "Mailbox Level Rarities", "All: 15, Modded: 15", "Rarities for Mailbox.");
            configMailboxCustomLevelRarities = Config.Bind("Mailbox Rarities", "Mailbox Custom Level Rarities", "", "Custom rarities for Mailbox.");
            configMailboxMinValue = Config.Bind("Mailbox", "Mailbox Min Value", 15, "Minimum value for Mailbox.");
            configMailboxMaxValue = Config.Bind("Mailbox", "Mailbox Max Value", 150, "Maximum value for Mailbox.");

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "something_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "something_assets")}");

            RegisterItem("GoodPolaroid", configGoodPolaroidMinValue, configGoodPolaroidMaxValue, configGoodPolaroidLevelRarities, configGoodPolaroidCustomLevelRarities);
            RegisterItem("BadPolaroid", configBadPolaroidMinValue, configBadPolaroidMaxValue, configBadPolaroidLevelRarities, configBadPolaroidCustomLevelRarities);
            RegisterItem("CursedPolaroid", configCursedPolaroidMinValue, configCursedPolaroidMaxValue, configCursedPolaroidLevelRarities, configCursedPolaroidCustomLevelRarities);
            RegisterItem("Keytar", configKeytarMinValue, configKeytarMaxValue, configKeytarLevelRarities, configKeytarCustomLevelRarities);
            RegisterItem("AubreyPlush", configAubreyPlushMinValue, configAubreyPlushMaxValue, configAubreyPlushLevelRarities, configAubreyPlushCustomLevelRarities);
            RegisterItem("BasilPlush", configBasilPlushMinValue, configBasilPlushMaxValue, configBasilPlushLevelRarities, configBasilPlushCustomLevelRarities);
            RegisterItem("Bunnybun", configBunnybunMinValue, configBunnybunMaxValue, configBunnybunLevelRarities, configBunnybunCustomLevelRarities);
            RegisterItem("Mailbox", configMailboxMinValue, configMailboxMaxValue, configMailboxLevelRarities, configMailboxCustomLevelRarities);


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
        void RegisterItem(string itemName, ConfigEntry<int> minValue, ConfigEntry<int> maxValue, ConfigEntry<string> levelRarities, ConfigEntry<string> customLevelRarities)
        {
            Item item = ModAssets!.LoadAsset<Item>($"Assets/ModAssets/{itemName}Item.asset");
            if (item == null) { LoggerInstance.LogError($"Error: Couldn't get {itemName}Item from assets"); return; }
            LoggerInstance.LogDebug($"Got {itemName} prefab");

            item.minValue = minValue.Value;
            item.maxValue = maxValue.Value;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            Utilities.FixMixerGroups(item.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(item, GetLevelRarities(levelRarities.Value), GetCustomLevelRarities(customLevelRarities.Value));
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
