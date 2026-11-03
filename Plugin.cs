using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;

namespace QuanTerraforming
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // Soft: makes Heightmap Unlimited load first when present, so TerrainLimits can see it and stand aside.
    [BepInDependency(TerrainLimits.HeightmapUnlimitedGuid, BepInDependency.DependencyFlags.SoftDependency)]
    // The sea fix changes wave height, which drives boat and float physics on whichever client owns the object,
    // so everyone needs the same behaviour.
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "quandru.QuanTerraforming";
        public const string PluginName = "QuanTerraforming";
        public const string PluginVersion = PluginInfo.Version; // generated from <Version> in the .csproj

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> UpperLimit;
        internal static ConfigEntry<float> LowerLimit;
        internal static ConfigEntry<bool> SeaFollowsTerrain;
        internal static ConfigEntry<int> Radius;

        private static ConfigDescription AdminOnly(string text, AcceptableValueBase range = null) =>
            new ConfigDescription(text + " Synced from the server; admins only.", range,
                new ConfigurationManagerAttributes { IsAdminOnly = true });

        private void Awake()
        {
            const string general = "0 - General";
            Enabled = Config.Bind(general, "Enabled", true, AdminOnly(
                "Untick to switch the whole mod off and go back to vanilla: waves keep the original seabed, the " +
                "terrain limit returns to 8 m and the wand shows nothing. Careful: land raised or dug more than 8 m " +
                "then shows cut back to 8 m, and any terrain edit there saves the cut."));

            const string heightmap = "1 - Heightmap";
            UpperLimit = Config.Bind(heightmap, "Upper limit", 200f, AdminOnly(
                "How far terrain can be raised above the ground as the world generated it, in metres. Vanilla is 8. " +
                "Lowering this below existing edits shows them cut down, and the next edit there saves the cut.",
                new AcceptableValueRange<float>(1f, 200f)));
            LowerLimit = Config.Bind(heightmap, "Lower limit", 200f, AdminOnly(
                "How far terrain can be dug below the ground as the world generated it, in metres. Vanilla is 8. " +
                "Same caution as the upper limit.",
                new AcceptableValueRange<float>(1f, 200f)));

            const string sea = "2 - Sea";
            SeaFollowsTerrain = Config.Bind(sea, "Waves follow terrain edits", true, AdminOnly(
                "Raising or lowering terrain changes how big the sea's waves are there, as the ground's height " +
                "suggests. Off is vanilla: waves keep the seabed as the world was generated, whatever you build. " +
                "Applies as zones load or are edited."));

            const string grid = "3 - Zone grid (personal)";
            Radius = Config.Bind(grid, "Radius", 3, new ConfigDescription(
                "While you hold the Terraforming Wand, how many zones of grid to draw on each side of the one you're " +
                "standing in. Personal.",
                new AcceptableValueRange<int>(1, 8)));

            new Harmony(PluginGuid).PatchAll(typeof(Plugin).Assembly);
            if (TerrainLimits.OtherModHandlesLimits)
                Logger.LogWarning("Heightmap Unlimited JvL is installed, so it sets the terrain limits and this mod's " +
                                  "\"1 - Heightmap\" settings do nothing. Remove it once this mod is on every client.");

            Wand.TerraformingWand.Register();
            gameObject.AddComponent<ZoneGridOverlay>();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
