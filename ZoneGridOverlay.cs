using System.Collections.Generic;
using UnityEngine;

namespace QuanZoneGrid
{
    // Draws zone (chunk) borders around the player and a coloured post at every zone corner.
    //
    // Why corners: WaterVolume scales wave height by the ocean depth at the four corners of its zone, blended
    // across the zone (WaterVolume.DetectWaterDepth / Depth). The depth is 30 m (sea level) minus the terrain
    // height at each corner vertex, worked out in Heightmap.UpdateCornerDepths and read here through
    // Heightmap.GetOceanDepth(), so the colours show exactly what the waves will use.
    // Green = at or above sea level (no waves), yellow to red = deeper, red = 10 m or more (full waves).
    internal class ZoneGridOverlay : MonoBehaviour
    {
        private const float ZoneSize = 64f;          // ZoneSystem.m_zoneSize
        private const float WaterLevel = 30f;        // ZoneSystem.m_waterLevel
        private const float FullWaveDepth = 10f;     // WaterVolume normalises depth / 10
        private const float SampleStep = 2f;
        private const float RefreshSeconds = 1f;

        public bool Visible { get; set; }

        private float _nextRefresh;
        private Material _material;
        private readonly List<LineRenderer> _pool = new List<LineRenderer>();
        private int _used;
        private Transform _root;
        private GUIStyle _hudStyle;
        private string _hud = "";

        // Deepest value across the zones sharing each corner, keyed by rounded world x/z.
        private readonly Dictionary<long, float> _cornerDepth = new Dictionary<long, float>();

        private void Awake()
        {
            _root = new GameObject("QuanZoneGridLines").transform;
            DontDestroyOnLoad(_root.gameObject);
            _root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
        }

        private void Update()
        {
            if (Plugin.ToggleKey.Value.IsDown() && !TypingInUi())
            {
                Visible = !Visible;
                _nextRefresh = 0f;
            }

            Player player = Player.m_localPlayer;
            bool show = Visible && player != null;
            if (_root.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
            if (!show || Time.time < _nextRefresh) return;

            _nextRefresh = Time.time + RefreshSeconds;
            Rebuild(player.transform.position);
        }

        private static bool TypingInUi()
        {
            return (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible() || TextInput.IsVisible();
        }

        private static long Key(float x, float z) => ((long)Mathf.RoundToInt(x) << 32) ^ (uint)Mathf.RoundToInt(z);

        private void ReadCornerDepths()
        {
            _cornerDepth.Clear();
            foreach (Heightmap hm in Heightmap.GetAllHeightmaps())
            {
                if (hm == null || hm.IsDistantLod) continue;
                float[] d = hm.GetOceanDepth();
                Vector3 c = hm.transform.position;
                float half = hm.m_width * hm.m_scale * 0.5f;
                // Index layout from Heightmap.UpdateCornerDepths: 0=(xmin,zmax) 1=(xmax,zmax) 2=(xmax,zmin) 3=(xmin,zmin)
                StoreCorner(c.x - half, c.z + half, d[0]);
                StoreCorner(c.x + half, c.z + half, d[1]);
                StoreCorner(c.x + half, c.z - half, d[2]);
                StoreCorner(c.x - half, c.z - half, d[3]);
            }
        }

        private void StoreCorner(float x, float z, float depth)
        {
            long k = Key(x, z);
            if (!_cornerDepth.TryGetValue(k, out float old) || depth > old) _cornerDepth[k] = depth;
        }

        private bool TryCornerDepth(float x, float z, out float depth) => _cornerDepth.TryGetValue(Key(x, z), out depth);

        private void Rebuild(Vector3 p)
        {
            ReadCornerDepths();
            _used = 0;

            int radius = Plugin.Radius.Value;
            int zx = Mathf.FloorToInt((p.x + ZoneSize / 2f) / ZoneSize);
            int zz = Mathf.FloorToInt((p.z + ZoneSize / 2f) / ZoneSize);
            float minX = (zx - radius) * ZoneSize - ZoneSize / 2f;
            float maxX = (zx + radius + 1) * ZoneSize - ZoneSize / 2f;
            float minZ = (zz - radius) * ZoneSize - ZoneSize / 2f;
            float maxZ = (zz + radius + 1) * ZoneSize - ZoneSize / 2f;
            Color lineColor = new Color(1f, 1f, 1f, 0.85f);

            for (float x = minX; x <= maxX + 0.1f; x += ZoneSize)
                GroundLine(new Vector3(x, 0f, minZ), new Vector3(x, 0f, maxZ), lineColor);
            for (float z = minZ; z <= maxZ + 0.1f; z += ZoneSize)
                GroundLine(new Vector3(minX, 0f, z), new Vector3(maxX, 0f, z), lineColor);

            for (float x = minX; x <= maxX + 0.1f; x += ZoneSize)
                for (float z = minZ; z <= maxZ + 0.1f; z += ZoneSize)
                    CornerPost(x, z);

            for (int i = _used; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);

            float cx = zx * ZoneSize, cz = zz * ZoneSize, h = ZoneSize / 2f;
            _hud = $"Zone ({zx}, {zz})   {Plugin.ToggleKey.Value} hides\n" +
                   $"NW {Describe(cx - h, cz + h)}    NE {Describe(cx + h, cz + h)}\n" +
                   $"SW {Describe(cx - h, cz - h)}    SE {Describe(cx + h, cz - h)}\n" +
                   "Waves re-read the corners when the zone reloads.";
        }

        private string Describe(float x, float z)
        {
            if (!TryCornerDepth(x, z, out float d)) return "not loaded";
            if (d <= 0f) return "above sea (0%)";
            return $"{d:0.0} m deep ({Mathf.Clamp01(d / FullWaveDepth) * 100f:0}%)";
        }

        private void GroundLine(Vector3 a, Vector3 b, Color color)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / SampleStep) + 1);
            LineRenderer lr = NextLine(0.25f, color);
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                Vector3 q = Vector3.Lerp(a, b, i / (float)(n - 1));
                float ground = Heightmap.GetHeight(q, out float height) ? height : WaterLevel;
                q.y = Mathf.Max(ground, WaterLevel) + 0.4f;
                lr.SetPosition(i, q);
            }
        }

        private void CornerPost(float x, float z)
        {
            Color color;
            float baseY;
            if (TryCornerDepth(x, z, out float d))
            {
                color = d <= 0f ? Color.green : Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01(d / FullWaveDepth));
                baseY = WaterLevel - d;
            }
            else
            {
                color = Color.grey;
                baseY = Heightmap.GetHeight(new Vector3(x, 0f, z), out float height) ? height : WaterLevel;
            }
            LineRenderer lr = NextLine(0.6f, color);
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(x, baseY, z));
            lr.SetPosition(1, new Vector3(x, Mathf.Max(baseY, WaterLevel) + 30f, z));
        }

        private LineRenderer NextLine(float width, Color color)
        {
            if (_used == _pool.Count)
            {
                var go = new GameObject("QuanZoneGridLine");
                go.transform.SetParent(_root, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.material = LineMaterial();
                _pool.Add(lr);
            }
            LineRenderer r = _pool[_used++];
            r.gameObject.SetActive(true);
            r.startWidth = r.endWidth = width;
            r.startColor = r.endColor = color;
            return r;
        }

        private Material LineMaterial()
        {
            if (_material != null) return _material;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("UI/Default");
            _material = new Material(shader);
            return _material;
        }

        private void OnGUI()
        {
            if (!Visible || Player.m_localPlayer == null || _hud.Length == 0) return;
            if (_hudStyle == null)
                _hudStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = false };
            GUI.Box(new Rect(20f, Screen.height * 0.35f, 540f, 100f), _hud, _hudStyle);
        }
    }
}
