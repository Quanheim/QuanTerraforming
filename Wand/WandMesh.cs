using System.Collections.Generic;
using UnityEngine;

namespace QuanTerraforming.Wand
{
    // The wand's model: a turned shape built in the same local space as the vanilla hammer's mesh (hammer_Cube.006,
    // on Hammer/attach/hammer), so it sits in the hand exactly where the hammer's handle does. That mesh runs along
    // +Y from the butt (-0.342) to the top of the shaft (1.228); this profile reuses that length.
    //
    // Two submeshes: 0 = grip and collar, textured from the hammer handle's patch of the shared "weapons" atlas;
    // 1 = the body, in the darkwood beams texture. The profile matches tools/render_wand.py (the approved mockup).
    internal static class WandMesh
    {
        private enum Part { Grip, Collar, Body }

        private struct Ring
        {
            public float Y, Radius;
            public Part Part;
            public Ring(float y, float radius, Part part) { Y = y; Radius = radius; Part = part; }
        }

        private static readonly Ring[] Profile =
        {
            new Ring(-0.342f, 0.048f, Part.Grip), new Ring(-0.326f, 0.066f, Part.Grip), new Ring(0.120f, 0.066f, Part.Grip),
            new Ring(0.130f, 0.080f, Part.Collar), new Ring(0.200f, 0.080f, Part.Collar),
            new Ring(0.210f, 0.0435f, Part.Body), new Ring(0.720f, 0.039f, Part.Body),   // first half: nearly straight
            new Ring(0.960f, 0.030f, Part.Body), new Ring(1.150f, 0.018f, Part.Body),    // last half: most of the taper
            new Ring(1.205f, 0.012f, Part.Body), new Ring(1.228f, 0f, Part.Body),        // blunt tip
        };

        internal const float ButtY = -0.342f;
        internal const float TipY = 1.228f;
        internal const float MaxRadius = 0.080f;

        // The hammer handle's area of the weapons atlas (u0, u1, v0, v1), read from the vanilla mesh's UVs.
        private static readonly Vector4 GripUv = new Vector4(0.807f, 0.834f, 0.627f, 0.734f);

        private const int Sides = 12;

        internal static Mesh Build()
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var grip = new List<int>();
            var body = new List<int>();

            // Butt cap: a flat disc facing -Y.
            {
                Ring r = Profile[0];
                int centre = verts.Count;
                verts.Add(new Vector3(0f, r.Y, 0f)); normals.Add(Vector3.down); uvs.Add(GripTex(0.5f, 0f));
                for (int i = 0; i <= Sides; i++)
                {
                    float a = i * Mathf.PI * 2f / Sides;
                    verts.Add(new Vector3(Mathf.Cos(a) * r.Radius, r.Y, Mathf.Sin(a) * r.Radius));
                    normals.Add(Vector3.down);
                    uvs.Add(GripTex(Mirror(i), 0f));
                }
                for (int i = 0; i < Sides; i++) { grip.Add(centre); grip.Add(centre + 1 + i); grip.Add(centre + 2 + i); }
            }

            // Each segment between two rings gets its own vertices, so the steps at the collar stay crisp.
            for (int s = 0; s < Profile.Length - 1; s++)
            {
                Ring a = Profile[s], b = Profile[s + 1];
                bool isBody = a.Part == Part.Body;
                List<int> tris = isBody ? body : grip;
                float slope = (a.Radius - b.Radius) / Mathf.Max(0.0001f, b.Y - a.Y);
                int start = verts.Count;
                for (int i = 0; i <= Sides; i++)
                {
                    float ang = i * Mathf.PI * 2f / Sides;
                    var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    Vector3 n = new Vector3(dir.x, slope, dir.z).normalized;
                    verts.Add(dir * a.Radius + Vector3.up * a.Y); normals.Add(n);
                    verts.Add(dir * b.Radius + Vector3.up * b.Y); normals.Add(n);
                    float m = Mirror(i);
                    if (isBody) { uvs.Add(BodyTex(m, a.Y)); uvs.Add(BodyTex(m, b.Y)); }
                    else
                    {
                        float shade = a.Part == Part.Collar ? 0.15f : 0f; // collar takes a slightly different patch
                        uvs.Add(GripTex(m, Along(a.Y) + shade)); uvs.Add(GripTex(m, Along(b.Y) + shade));
                    }
                }
                for (int i = 0; i < Sides; i++)
                {
                    int v0 = start + i * 2, v1 = v0 + 1, v2 = v0 + 2, v3 = v0 + 3;
                    tris.Add(v0); tris.Add(v1); tris.Add(v2);
                    tris.Add(v2); tris.Add(v1); tris.Add(v3);
                }
            }

            var mesh = new Mesh { name = "QuanTerraformingWand" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(grip, 0);
            mesh.SetTriangles(body, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // 0 -> 1 -> 0 around the circumference, so the texture has no seam.
        private static float Mirror(int i) => 1f - Mathf.Abs(i * 2f / Sides - 1f);

        // 0 at the butt, 1 at the collar's far edge.
        private static float Along(float y) => Mathf.Clamp01((y - ButtY) / (0.21f - ButtY));

        private static Vector2 GripTex(float around, float along) =>
            new Vector2(Mathf.Lerp(GripUv.x, GripUv.y, around), Mathf.Lerp(GripUv.z, GripUv.w, Mathf.Clamp01(along)));

        // The dark, stained left half of DarkWoodBeams, running twice along the body's length.
        private static Vector2 BodyTex(float around, float y) =>
            new Vector2(0.03f + 0.38f * around, (y - 0.21f) / 0.54f * 0.5f);
    }
}
