using System.IO;
using System.Reflection;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace QuanTerraforming.Wand
{
    // The Terraforming Wand: a clone of the vanilla Hammer with its head replaced by a turned wooden wand (WandMesh).
    // Version 1 has no tools: it isn't a build tool (no piece table), it can't swing, and holding it shows the zone
    // grid. The shaping tools in docs/design.md will give it a piece table of its own.
    internal static class TerraformingWand
    {
        internal const string PrefabName = "QuanTerraformingWand";
        internal const string NameToken = "$item_quanterraforming_wand";
        private const string DescriptionToken = "$item_quanterraforming_wand_description";

        private const string HammerVisualPath = "attach/hammer";   // Hammer prefab child holding its mesh
        private const string DarkWoodMaterial = "DarkWood_mat";    // darkwood beams and roof

        internal static void Register()
        {
            LocalizationManager.Instance.GetLocalization().AddTranslation("English", new System.Collections.Generic.Dictionary<string, string>
            {
                { NameToken.TrimStart('$'), "Terraforming Wand" },
                { DescriptionToken.TrimStart('$'),
                  "A wand for shaping the land, from QuanTerraforming. Hold it to see the zone grid: the borders of " +
                  "each 64 m zone, and how deep the sea is at each corner, which is what sets wave height there. " +
                  "Shaping tools are on the way." },
            });
            PrefabManager.OnVanillaPrefabsAvailable += AddItem;
        }

        private static void AddItem()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddItem;

            var config = new ItemConfig
            {
                Name = NameToken,
                Description = DescriptionToken,
                CraftingStation = null,   // made from the inventory crafting panel, like the hammer
                Requirements = new[] { new RequirementConfig("Wood", 2) },
            };
            Sprite icon = LoadIcon();
            if (icon != null) config.Icons = new[] { icon };

            var item = new CustomItem(PrefabName, "Hammer", config);
            if (!item.IsValid())
            {
                Jotunn.Logger.LogError("Terraforming Wand: couldn't clone the vanilla Hammer.");
                return;
            }

            GameObject prefab = item.ItemPrefab;
            ItemDrop.ItemData.SharedData shared = prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_buildPieces = null;      // not a build tool (yet)
            shared.m_useDurability = false;
            shared.m_canBeReparied = false;
            shared.m_weight = 0.5f;

            Reshape(prefab);
            ItemManager.Instance.AddItem(item);
        }

        // Swap the hammer's mesh for the wand's, keeping its transform so the hand holds it in the same place.
        private static void Reshape(GameObject prefab)
        {
            Transform visual = prefab.transform.Find(HammerVisualPath);
            MeshFilter filter = visual != null ? visual.GetComponent<MeshFilter>() : null;
            MeshRenderer renderer = visual != null ? visual.GetComponent<MeshRenderer>() : null;
            if (filter == null || renderer == null)
            {
                Jotunn.Logger.LogError($"Terraforming Wand: Hammer has no mesh at {HammerVisualPath}; the wand will look like a hammer.");
                return;
            }

            Material grip = renderer.sharedMaterial;   // "weapons1", the shared atlas the hammer handle uses
            Material body = PrefabManager.Cache.GetPrefab<Material>(DarkWoodMaterial);
            if (body == null)
            {
                Jotunn.Logger.LogWarning($"Terraforming Wand: material {DarkWoodMaterial} not found; the body uses the grip's wood.");
                body = grip;
            }

            filter.sharedMesh = WandMesh.Build();
            renderer.sharedMaterials = new[] { grip, body };

            // The dropped item's collider was sized for the hammerhead. Fit it to the wand: the mesh runs along local
            // +Y, which the visual's rotation turns to +Z on the prefab.
            Transform colliderHolder = prefab.transform.Find("collider");
            BoxCollider box = colliderHolder != null ? colliderHolder.GetComponent<BoxCollider>() : null;
            if (box != null)
            {
                colliderHolder.localScale = Vector3.one;
                Bounds b = filter.sharedMesh.bounds;
                Vector3 min = prefab.transform.InverseTransformPoint(visual.TransformPoint(b.min));
                Vector3 max = prefab.transform.InverseTransformPoint(visual.TransformPoint(b.max));
                Vector3 lo = Vector3.Min(min, max), hi = Vector3.Max(min, max);
                box.center = colliderHolder.InverseTransformPoint(prefab.transform.TransformPoint((lo + hi) / 2f));
                box.size = new Vector3(Mathf.Max(hi.x - lo.x, 0.05f), Mathf.Max(hi.y - lo.y, 0.05f), Mathf.Max(hi.z - lo.z, 0.05f));
            }
        }

        private static Sprite LoadIcon()
        {
            Assembly asm = typeof(TerraformingWand).Assembly;
            using (Stream stream = asm.GetManifestResourceStream("QuanTerraforming.Assets.wand_icon.png"))
            {
                if (stream == null) { Jotunn.Logger.LogWarning("Terraforming Wand: icon resource missing."); return null; }
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                // ImageConversion.LoadImage(Texture2D, byte[]), by reflection (see the .csproj).
                MethodInfo load = System.Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
                    ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
                if (load == null || !(bool)load.Invoke(null, new object[] { tex, bytes }))
                {
                    Jotunn.Logger.LogWarning("Terraforming Wand: couldn't load the icon; it keeps the hammer's.");
                    return null;
                }
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }

        internal static bool IsWand(ItemDrop.ItemData item) => item != null && item.m_shared.m_name == NameToken;

        internal static bool HeldByLocalPlayer()
        {
            Player player = Player.m_localPlayer;
            return player != null && IsWand(player.GetCurrentWeapon());
        }

        // The hammer's attack would swing it like a club. The wand doesn't attack.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static class NoSwing
        {
            private static bool Prefix(Humanoid __instance, ref bool __result)
            {
                if (!IsWand(__instance.GetCurrentWeapon())) return true;
                __result = false;
                return false;
            }
        }
    }
}
