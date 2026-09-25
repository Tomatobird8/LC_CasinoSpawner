using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace LethalCasinoSpawner
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mrgrm7.LethalCasino")]
    public class LethalCasinoSpawner : BaseUnityPlugin
    {
        public static LethalCasinoSpawner Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static ConfigEntry<bool> terminalEnabled = null!;

        public static ConfigEntry<bool> configEnabled = null!;

        public static List<ConfigEntry<string>> moonConfigurations = [];

        internal static Scene latestScene;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            terminalEnabled = Config.Bind("General", "Enable Terminal Commands", true, "Whether the terminal commands for spawning/despawning casino should be enabled. Host only.");
            configEnabled = Config.Bind("General", "Enable Per Moon Configs", true, "Whether the per-moon configuration for spawning casino assets should be enabled. Host only.");

            Patch();

            SceneManager.sceneLoaded += OnSceneLoad;

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }


        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            latestScene = scene;
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
