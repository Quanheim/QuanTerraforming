using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace QuanTerraforming
{
    // Vanilla limits terrain edits to 8 m above or below the world-generated ground: LevelTerrain and RaiseTerrain
    // clamp the stored height change to -8..8, and ApplyToHeightmap clamps the final height to base ± 8 when drawing.
    // These transpilers swap those constants for the server's settings (replaces Heightmap Unlimited JvL).
    [HarmonyPatch(typeof(TerrainComp))]
    internal static class TerrainLimits
    {
        internal const string HeightmapUnlimitedGuid = "digitalroot.mods.heightmapunlimitedjvl";

        // Stand aside while Heightmap Unlimited is installed: it has already replaced the constants, and two mods
        // patching the same lines would leave whichever ran second doing nothing, or both half-applied.
        internal static bool OtherModHandlesLimits => Chainloader.PluginInfos.ContainsKey(HeightmapUnlimitedGuid);

        private static bool Prepare() => !OtherModHandlesLimits;

        private const float Vanilla = 8f;

        // Read every time the game clamps, so a synced change applies straight away. Mod switched off: vanilla.
        public static float MaxRaise() => Plugin.Enabled.Value ? Plugin.UpperLimit.Value : Vanilla;
        public static float MaxDigNegative() => -MaxDig();
        public static float MaxDig() => Plugin.Enabled.Value ? Plugin.LowerLimit.Value : Vanilla;

        // Stored height change: Mathf.Clamp(delta, -8f, 8f).
        [HarmonyTranspiler]
        [HarmonyPatch("LevelTerrain")]
        private static IEnumerable<CodeInstruction> LevelTerrain(IEnumerable<CodeInstruction> code) =>
            ReplaceDeltaClamp(code, "LevelTerrain");

        [HarmonyTranspiler]
        [HarmonyPatch("RaiseTerrain")]
        private static IEnumerable<CodeInstruction> RaiseTerrain(IEnumerable<CodeInstruction> code) =>
            ReplaceDeltaClamp(code, "RaiseTerrain");

        // Final height: Mathf.Clamp(height, baseHeight - 8f, baseHeight + 8f). The first 8 is the dig limit.
        [HarmonyTranspiler]
        [HarmonyPatch("ApplyToHeightmap")]
        private static IEnumerable<CodeInstruction> ApplyToHeightmap(IEnumerable<CodeInstruction> code)
        {
            var result = new List<CodeInstruction>(code);
            int found = 0;
            for (int i = 0; i < result.Count; i++)
            {
                if (!result[i].Is(OpCodes.Ldc_R4, 8f)) continue;
                string getter = found == 0 ? nameof(MaxDig) : nameof(MaxRaise);
                result[i] = Call(result[i], getter);
                found++;
            }
            Check("ApplyToHeightmap", found);
            return result;
        }

        private static IEnumerable<CodeInstruction> ReplaceDeltaClamp(IEnumerable<CodeInstruction> code, string method)
        {
            var result = new List<CodeInstruction>(code);
            int found = 0;
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].Is(OpCodes.Ldc_R4, -8f)) { result[i] = Call(result[i], nameof(MaxDigNegative)); found++; }
                else if (result[i].Is(OpCodes.Ldc_R4, 8f)) { result[i] = Call(result[i], nameof(MaxRaise)); found++; }
            }
            Check(method, found);
            return result;
        }

        // Keep labels and exception blocks on the replaced instruction.
        private static CodeInstruction Call(CodeInstruction original, string getter) =>
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TerrainLimits), getter)).MoveLabelsFrom(original).MoveBlocksFrom(original);

        private static void Check(string method, int found)
        {
            if (found != 2)
                Jotunn.Logger.LogError($"Terrain limits: expected two 8 m limits in TerrainComp.{method}, found {found}. " +
                                       "Valheim may have changed; the limit there may still be 8 m.");
        }
    }
}
