using HarmonyLib;
using LethalCasino.Custom;
using Unity.Netcode;
using UnityEngine;

namespace LethalCasinoSpawner.Patches;

[HarmonyPatch]
internal class CasinoSpawner
{
    static readonly string[] validObjects =
    [
        "CasinoBuilding",
        "SlotMachine",
        "Roulette",
        "Jukebox",
        "ATM",
        "Blackjack",
        "TheWheel"
    ];

    static readonly (string Key, string Prefab, Vector3 LocalPosition, Quaternion LocalRotation)[] casinoBuildingData =
    {
        ("CasinoBuilding", "CasinoBuilding", new Vector3(-16.8f, -1.6f, 8.3227f), Quaternion.Euler(0f, 180f, 0f)),
        ("SlotMachine1", "SlotMachine", new Vector3(-20.5584f, -2.5291f, 4.9645f), Quaternion.Euler(0f, 0f, 0f)),
        ("SlotMachine2", "SlotMachine", new Vector3(-23.6093f, -2.5291f, 4.9645f), Quaternion.Euler(0f, 0f, 0f)),
        ("SlotMachine3", "SlotMachine", new Vector3(-26.6712f, -2.5291f, 4.9645f), Quaternion.Euler(0f, 0f, 0f)),
        ("Roulette", "Roulette", new Vector3(-26.9403f, -2.5291f, 11.2909f), Quaternion.Euler(0f, 90f, 0f)),
        ("Jukebox", "Jukebox", new Vector3(-16.5368f, -0.3655f, 5.31f), Quaternion.Euler(0f, 0f, 0f)),
        ("ATM", "ATM", new Vector3(-8.5513f, -1.6819f, 5.0336f), Quaternion.Euler(0f, 0f, 0f)),
        ("Blackjack", "Blackjack", new Vector3(-20.9403f, -2.5291f, 11.2909f), Quaternion.Euler(0f, -90f, 0f)),
        ("TheWheel", "TheWheel", new Vector3(-23.9859f, -2.518f, 18.2677f), Quaternion.Euler(0f, 180f, 0f))
    };

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.ParsePlayerSentence))]
    [HarmonyPostfix]
    private static void ParsePlayerSentence_Postfix(Terminal __instance, ref TerminalNode __result)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!LethalCasinoSpawner.terminalEnabled.Value) return;
        if (StartOfRound.Instance.inShipPhase)
        {
            __result = ScriptableObject.CreateInstance<TerminalNode>();
            __result.displayText = "Cannot spawn casino in orbit.";
            __result.clearPreviousText = true;
            return;
        }

        string[] text = __instance.screenText.text[^__instance.textAdded..].Split(" ");
        if (text.Length == 0) return;

        string command = text[0];
        if (command.Trim().ToLowerInvariant() != "casino") return;
        if (text.Length == 1)
        {
            __result = ScriptableObject.CreateInstance<TerminalNode>();
            __result.displayText = "Usage:\ncasino spawn [x y z] [y] - Spawns a casino (at position and y-rotation if specified)\ncasino despawn - Despawns all casino objects\n\n";
            __result.clearPreviousText = true;
            return;
        }
        CasinoManager manager = LethalCasino.patches.RoundManagerPatch.CasinoManager;

        string mainArg = text[1].Trim().ToLowerInvariant();

        if (mainArg == "spawn")
        {
            if (text.Length != 2 && text.Length != 5 && text.Length != 6)
            {
                __result = ScriptableObject.CreateInstance<TerminalNode>();
                __result.displayText = "Please provide valid command arguments!\nUsage: casino spawn [x y z] [y]\nExamples:\ncasino spawn\ncasino spawn -100 15 0.24\ncasino spawn 0 10 0 270\n\n";
                __result.clearPreviousText = true;
                return;
            }
            __result = ScriptableObject.CreateInstance<TerminalNode>();
            __result.displayText = "Spawning casino!\n\n";
            __result.clearPreviousText = true;

            manager.SerializeAndSendHostConfigs();

            if (text.Length == 2)
            {
                SpawnEntireCasino(casinoBuildingData[0].LocalPosition, Quaternion.identity);
                LethalCasinoSpawner.Logger.LogInfo("Casino spawned at default position.");
            } else if (text.Length >= 5)
            {
                Vector3 offsetPosition = Vector3.zero;
                Quaternion offsetRotation = Quaternion.identity;
                if (float.TryParse(text[2], out float offsetX))
                {
                    offsetPosition.x = offsetX;
                }
                if (float.TryParse(text[3], out float offsetY))
                {
                    offsetPosition.y = offsetY;
                }
                if (float.TryParse(text[4], out float offsetZ))
                {
                    offsetPosition.z = offsetZ;
                }
                if (text.Length == 6 && float.TryParse(text[5], out float rotOffsetY))
                {
                    offsetRotation = Quaternion.Euler(0, rotOffsetY, 0);
                }

                SpawnEntireCasino(offsetPosition, offsetRotation);
            }
        }

        if (mainArg == "despawn" || mainArg == "destroy" || mainArg == "delete" || mainArg == "remove")
        {
            if (CasinoManager.Objects.Count == 0)
            {
                __result = ScriptableObject.CreateInstance<TerminalNode>();
                __result.displayText = "No casino objects to remove!\n\n";
                __result.clearPreviousText = true;
                return;
            }
            __result = ScriptableObject.CreateInstance<TerminalNode>();
            __result.displayText = "Removing casino!\n\n\n";
            __result.clearPreviousText = true;

            manager.DespawnCasinoServerRpc();

            return;
        }
    }

    
    [HarmonyPatch(typeof(RoundManager) ,nameof(RoundManager.LoadNewLevelWait))]
    [HarmonyAfter("mrgrm7.LethalCasino")]
    [HarmonyPrefix]
    private static void LoadNewLevelWait_Prefix(RoundManager __instance)
    {
        for (int i = 0; i < LethalCasinoSpawner.moonConfigurations.Count; i++)
        {
            if (LethalCasinoSpawner.moonConfigurations[i].Definition.Key == $"Moon: {__instance.currentLevel.PlanetName}")
            {
                ReadMoonConfig(LethalCasinoSpawner.moonConfigurations[i].Value);
                break;
            }
        }
    }

    private static void ReadMoonConfig(string s)
    {
        string[] spawnDefinitions = s.Split(';');
        if (spawnDefinitions.Length == 0)
        {
            LethalCasinoSpawner.Logger.LogInfo("Nothing defined in configuration. Skipping...");
            return; 
        }
        for (int i = 0;i < spawnDefinitions.Length; i++)
        {
            string[] spawnDefintion = spawnDefinitions[i].Split(",");
            if (spawnDefintion.Length != 7 && spawnDefintion.Length != 4)
            {
                LethalCasinoSpawner.Logger.LogError("Invalid spawn definition. Please use the correct formatting as seen in the description of the configuration. Invalid configuration: " + spawnDefinitions[i]);
                continue;
            }
            string objectName = spawnDefintion[0].Trim().ToLowerInvariant();

            if (!float.TryParse(spawnDefintion[1], out float objectPosX))
            {
                LethalCasinoSpawner.Logger.LogError($"Couldn't parse x position from {spawnDefinitions[i]}");
            }
            if (!float.TryParse(spawnDefintion[2], out float objectPosY))
            {
                LethalCasinoSpawner.Logger.LogError($"Couldn't parse y position from {spawnDefinitions[i]}");
            }
            if (!float.TryParse(spawnDefintion[3], out float objectPosZ))
            {
                LethalCasinoSpawner.Logger.LogError($"Couldn't parse z position from {spawnDefinitions[i]}");
            }
            Quaternion objectRotation = Quaternion.identity;
            if (spawnDefintion.Length == 7)
            {
                if (!float.TryParse(spawnDefintion[4], out float objectRotX))
                {
                    LethalCasinoSpawner.Logger.LogError($"Couldn't parse x rotation from {spawnDefinitions[i]}");
                }
                if (!float.TryParse(spawnDefintion[5], out float objectRotY))
                {
                    LethalCasinoSpawner.Logger.LogError($"Couldn't parse y rotation from {spawnDefinitions[i]}");
                }
                if (!float.TryParse(spawnDefintion[6], out float objectRotZ))
                {
                    LethalCasinoSpawner.Logger.LogError($"Couldn't parse z rotation from {spawnDefinitions[i]}");
                }
                objectRotation = Quaternion.Euler(objectRotX, objectRotY, objectRotZ);
            }

            Vector3 objectPosition = new(objectPosX, objectPosY, objectPosZ);

            if (objectName == "casino")
            {
                SpawnEntireCasino(objectPosition, objectRotation);
            }
            else
            {
                SpawnCasinoObject(objectName, objectPosition, objectRotation);
            }
        }
    }

    private static void SpawnCasinoObject(string objectName, Vector3 objectPosition, Quaternion objectRotation)
    {
        bool found = false;
        for (int i = 0;i < validObjects.Length; i++)
        {
            if (objectName != validObjects[i].ToLowerInvariant())
            {
                continue;
            }
            found = true;
            for (int j = 0; j < 9999; j++)
            {
                if (CasinoManager.Objects.ContainsKey($"{validObjects[i]}{j}"))
                {
                    continue;
                }
                CasinoManager.Objects.Add($"{validObjects[i]}{j}", CasinoManager.Spawn(validObjects[i], objectPosition, objectRotation));
                break;
            }
            LethalCasinoSpawner.Logger.LogInfo($"{validObjects[i]} spawned at {objectPosition} with rotation {objectRotation}");
        }

        if (!found)
        {
            LethalCasinoSpawner.Logger.LogError($"'{objectName}' did not match any available valid object name. Please provide a valid object name. You can find them in the description of any moon configuration entry.");
        }
    }

    private static void SpawnEntireCasino(Vector3 offsetPosition, Quaternion offsetRotation)
    {
        Vector3 centerOffset = casinoBuildingData[0].LocalPosition;

        foreach ((string Key, string Prefab, Vector3 LocalPosition, Quaternion LocalRotation) in casinoBuildingData)
        {
            Vector3 centeredLocalPosition = LocalPosition - centerOffset;

            Vector3 worldPosition = offsetPosition + (offsetRotation * centeredLocalPosition);
            Quaternion worldRotation = offsetRotation * LocalRotation;

            LethalCasinoSpawner.Logger.LogDebug($"Prefab: {Prefab}, centeredLocalPosition: {centeredLocalPosition}, worldPosition: {worldPosition}, worldRotation: {worldRotation.eulerAngles}");

            for (int i = 0; i < 9999; i++)
            {
                if (CasinoManager.Objects.ContainsKey($"{Key}{i}"))
                {
                    continue;
                }
                CasinoManager.Objects.Add($"{Key}{i}", CasinoManager.Spawn(Prefab, worldPosition, worldRotation));
                break;
            }
        }
        LethalCasinoSpawner.Logger.LogInfo($"Casino spawned at position {offsetPosition} with rotation {offsetRotation}");
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
    [HarmonyAfter("mrgrm7.LethalCasino")]
    [HarmonyPostfix]
    private static void DespawnPropsAtEndOfRound_Postfix(RoundManager __instance)
    {
        if (__instance.currentLevel.levelID != 3)
        {
            if (NetworkManager.Singleton.IsServer && CasinoManager.Objects.Count > 0)
            {
                LethalCasinoSpawner.Logger.LogInfo("Despawning casino objects");
                LethalCasino.patches.RoundManagerPatch.CasinoManager.DespawnCasinoServerRpc();
            }
        }
    }
}
