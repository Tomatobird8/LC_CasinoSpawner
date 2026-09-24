using HarmonyLib;
using LethalCasino.Custom;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;

namespace LethalCasinoSpawner.Patches;
[HarmonyPatch(typeof(CasinoBuilding))]
internal class CasinoBuildingPatcher
{
    [HarmonyPatch(nameof(CasinoBuilding.Awake))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Awake_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        var getSceneByNameMethod = AccessTools.Method(
            typeof(SceneManager),
            nameof(SceneManager.GetSceneByName),
            [typeof(string)]
        );

        var getLatestLoadedSceneMethod = AccessTools.Method(
            typeof(CasinoBuildingPatcher),
            nameof(GetLastLoadedScene)
        );

        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "CompanyBuilding"), new CodeMatch(OpCodes.Call, getSceneByNameMethod))
            .ThrowIfNotMatch("Couldn't find GetSceneByName pattern in CasinoBuilding.Awake()")
            .RemoveInstruction()
            .SetInstruction(new CodeInstruction(OpCodes.Call, getLatestLoadedSceneMethod))
            .InstructionEnumeration();
    }

    internal static Scene GetLastLoadedScene()
    {
        LethalCasinoSpawner.Logger.LogDebug($"GetLastLoadedScene was called using patched CasinoBuilding.Awake(). Last loaded scene name: {LethalCasinoSpawner.latestScene.name}");
        return LethalCasinoSpawner.latestScene;
    }

    [HarmonyPatch(nameof(CasinoBuilding.Update))]
    [HarmonyPrefix]
    internal static bool Update_Prefix(CasinoBuilding __instance)
    {
        if (__instance.fog == null)
            return false;
        return true;
    }
}
