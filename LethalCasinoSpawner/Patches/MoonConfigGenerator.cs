using HarmonyLib;

namespace LethalCasinoSpawner.Patches;
[HarmonyPatch(typeof(StartOfRound))]
internal class MoonConfigGenerator
{
    [HarmonyPatch(nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    internal static void GenerateMoonConfigs(StartOfRound __instance)
    {
        LethalCasinoSpawner.Logger.LogDebug("Generating configurations for each moon.");
        SelectableLevel[] levels = __instance.levels;

        for (int i = 0;i < levels.Length; i++)
        {
            LethalCasinoSpawner.moonConfigurations.Add(LethalCasinoSpawner.Instance.Config.Bind("Spawning", $"Moon: {levels[i].PlanetName}", "", "What type of casino object to spawn and where? Valid objects: Casino, CasinoBuilding, SlotMachine, Roulette, Jukebox, ATM, Blackjack, TheWheel. Provide 3d position and, optionally, rotation for each object. Example configuration: 'SlotMachine,25.5,10,10;Roulette,31.1,10,11.5,0,180,0;ATM,30,9.5,8'"));
        }
    }
}
