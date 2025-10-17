using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

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
        public static bool IsServer { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }

        public static AssetBundle? ModAssets;

        // Configs
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        // General
        public static ConfigEntry<bool> configSpoilerFreeVersion;

        // Something Configs
        public static ConfigEntry<string> configSomethingLevelRarities;
        public static ConfigEntry<string> configSomethingCustomLevelRarities;

        // Rabbit Configs
        public static ConfigEntry<string> configRabbitLevelRarities;
        public static ConfigEntry<string> configRabbitCustomLevelRarities;

        // Experiment 667 Configs
        public static ConfigEntry<string> configSpringCatLevelRarities;
        public static ConfigEntry<string> configSpringCatCustomLevelRarities;

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

            // General
            configSpoilerFreeVersion = Config.Bind("Spoilers", "Spoiler-Free Version", true, "Replaces most spoilers for the game with alternatives.");

            // BreathingUI
            //configShowBreathingTooltip = Config.Bind("BreathingUI", "Show Breathing Tooltip", true, "Shows the breathing tooltip on the top right when you are being haunted by Something.");

            // Something Configs
            configSomethingLevelRarities = Config.Bind("Rarities", "Something Level Rarities", "All: 40, Modded: 40", "Rarities for each level. See default for formatting.");
            configSomethingCustomLevelRarities = Config.Bind("Rarities", "Something Custom Level Rarities", "", "Rarities for modded levels.");

            // Rabbit
            configRabbitLevelRarities = Config.Bind("Rarities", "Rabbit Level Rarities", "All: 5, Modded: 5", "Rarities for each level. See default for formatting.");
            configRabbitCustomLevelRarities = Config.Bind("Rarities", "Rabbit Custom Level Rarities", "", "Rarities for modded levels.");

            // SpringCat
            configSpringCatLevelRarities = Config.Bind("Rarities", "Experiment 667 Level Rarities", "All: 25, Modded: 300", "Rarities for each level. See default for formatting.");
            configSpringCatCustomLevelRarities = Config.Bind("Rarities", "Experiment 667 Custom Level Rarities", "", "Rarities for modded levels.");

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
            configCursedPolaroidMinValue = Config.Bind("Polaroids", "Cursed Polaroid Min Value", 100, "Minimum value for Cursed Polaroid.");
            configCursedPolaroidMaxValue = Config.Bind("Polaroids", "Cursed Polaroid Max Value", 200, "Maximum value for Cursed Polaroid.");
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
            configMailboxMaxValue = Config.Bind("Mailbox", "Mailbox Max Value", 100, "Maximum value for Mailbox.");

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "something_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "something_assets")}");

            Utils.RegisterItem("Assets/ModAssets/Polaroids/GoodPolaroidItem.asset", configGoodPolaroidLevelRarities.Value, configGoodPolaroidCustomLevelRarities.Value, configGoodPolaroidMinValue.Value, configGoodPolaroidMaxValue.Value);
            Utils.RegisterItem("Assets/ModAssets/Polaroids/BadPolaroidItem.asset", configBadPolaroidLevelRarities.Value, configBadPolaroidCustomLevelRarities.Value, configBadPolaroidMinValue.Value, configBadPolaroidMaxValue.Value);
            Utils.RegisterItem("Assets/ModAssets/Polaroids/CursedPolaroidItem.asset",  configCursedPolaroidLevelRarities.Value, configCursedPolaroidCustomLevelRarities.Value, configCursedPolaroidMinValue.Value, configCursedPolaroidMaxValue.Value);
            Utils.RegisterItem("Assets/ModAssets/Keytar/KeytarItem.asset", configKeytarLevelRarities.Value, configKeytarCustomLevelRarities.Value, configKeytarMinValue.Value, configKeytarMaxValue.Value);
            Utils.RegisterItem("Assets/ModAssets/AubreyPlush/AubreyPlushItem.asset", configAubreyPlushLevelRarities.Value, configAubreyPlushCustomLevelRarities.Value, configAubreyPlushMinValue.Value, configAubreyPlushMaxValue.Value);
            Utils.RegisterItem("Assets/ModAssets/BasilPlush/BasilPlushItem.asset", configBasilPlushLevelRarities.Value, configBasilPlushCustomLevelRarities.Value, configBasilPlushMinValue.Value, configBasilPlushMaxValue.Value);
            Utils.RegisterItem("Assets/ModAssets/Bunnybun/BunnybunItem.asset", configBunnybunLevelRarities.Value, configBunnybunCustomLevelRarities.Value, configBunnybunMinValue.Value, configBunnybunMaxValue.Value);
            Utils.RegisterItem("Assets/ModAssets/Mailbox/MailboxItem.asset", configMailboxLevelRarities.Value, configMailboxCustomLevelRarities.Value, configMailboxMinValue.Value, configMailboxMaxValue.Value);


            EnemyType something = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/Something/SomethingEnemy.asset");
            if (something == null) { LoggerInstance.LogError("Error: Couldnt get Something enemy from assets"); return; }
            LoggerInstance.LogDebug($"Got Something enemy prefab");
            TerminalNode SomethingTN = ModAssets.LoadAsset<TerminalNode>("Assets/ModAssets/Something/SomethingTN.asset");
            TerminalKeyword SomethingTK = ModAssets.LoadAsset<TerminalKeyword>("Assets/ModAssets/Something/SomethingTK.asset");
            LoggerInstance.LogDebug("Registering enemy network prefab...");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(something.enemyPrefab);
            LoggerInstance.LogDebug("Registering enemy...");
            Enemies.RegisterEnemy(something, Utils.GetLevelRarities(configSomethingLevelRarities.Value), Utils.GetCustomLevelRarities(configSomethingCustomLevelRarities.Value), SomethingTN, SomethingTK);


            EnemyType rabbit = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/Rabbit/RabbitEnemy.asset");
            if (rabbit == null) { LoggerInstance.LogError("Error: Couldnt get Rabbit enemy from assets"); return; }
            LoggerInstance.LogDebug($"Got Rabbit enemy prefab");
            TerminalNode RabbitTN = ModAssets.LoadAsset<TerminalNode>("Assets/ModAssets/Rabbit/RabbitTN.asset");
            TerminalKeyword RabbitTK = ModAssets.LoadAsset<TerminalKeyword>("Assets/ModAssets/Rabbit/RabbitTK.asset");
            LoggerInstance.LogDebug("Registering enemy network prefab...");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(rabbit.enemyPrefab);
            LoggerInstance.LogDebug("Registering enemy...");
            Enemies.RegisterEnemy(rabbit, Utils.GetLevelRarities(configRabbitLevelRarities.Value), Utils.GetCustomLevelRarities(configRabbitCustomLevelRarities.Value), RabbitTN, RabbitTK);


            EnemyType springCat = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/SpringCat/SpringCatEnemy.asset");
            if (springCat == null) { LoggerInstance.LogError("Error: Couldnt get SpringCat enemy from assets"); return; }
            LoggerInstance.LogDebug($"Got SpringCat enemy prefab");
            TerminalNode SpringCatTN = ModAssets.LoadAsset<TerminalNode>("Assets/ModAssets/SpringCat/SpringCatTN.asset");
            TerminalKeyword SpringCatTK = ModAssets.LoadAsset<TerminalKeyword>("Assets/ModAssets/SpringCat/SpringCatTK.asset");
            LoggerInstance.LogDebug("Registering enemy network prefab...");
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(springCat.enemyPrefab);
            LoggerInstance.LogDebug("Registering enemy...");
            Enemies.RegisterEnemy(springCat, Utils.GetLevelRarities(configSpringCatLevelRarities.Value), Utils.GetCustomLevelRarities(configSpringCatCustomLevelRarities.Value), SpringCatTN, SpringCatTK);

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
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
