using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Turns the artist's 3D models into the 2D pictures the game uses.
// Menu: Filip > Bake Rocks And Stars From FBX.
//
//   Assets/Art/Source/stone.fbx -> Assets/Resources/Art/asteroid_l1..4, asteroid_m1..4, asteroid_s1..4 .png
//   Assets/Art/Source/stars.fbx -> Assets/Resources/Art/star1.png + star_glow1.png ... (one pair per model)
//
// The pictures are overwritten in place (same names, same .meta), so the prefabs that use
// them update by themselves. Drawing is done in code (no camera, no lights), so the result
// is the same on every computer and matches the earlier pictures:
//  - Rocks: lit mostly "from the camera" (still looks right when the game spins the rock),
//    rocky micro-relief, ambient occlusion and a warm rim. The rock is scaled so that a circle
//    of 0.45 x picture size has the same area as the rock: Asteroid uses exactly that circle.
//  - Stars: flat white shape in the middle 42% of the picture, plus a soft glow picture on the
//    same canvas. The game tints both.
public static class ArtBake
{
    const string StoneModel = ArtImportSettings.ModelFolder + "stone.fbx";
    const string StarsModel = ArtImportSettings.ModelFolder + "stars.fbx";
    const string OutFolder = "Assets/Resources/Art/";
    const int StarSize = 256;

    // Picture name, model inside stone.fbx, view angles in degrees (x, y, z), picture size in pixels.
    // Edit this list to add variants (asteroid_m5 ...); Art.LoadAll picks up numbers 1..9.
    struct RockJob
    {
        public string name, model; public double rx, ry, rz; public int size;
        public RockJob(string name, string model, double rx, double ry, double rz, int size)
        { this.name = name; this.model = model; this.rx = rx; this.ry = ry; this.rz = rz; this.size = size; }
    }

    static readonly RockJob[] RockJobs =
    {
        new RockJob("asteroid_l1", "m1",       0,   0,   0, 512), new RockJob("asteroid_l2", "m2",      30, -70,  60, 512),
        new RockJob("asteroid_l3", "m1",      70,  35,   0, 512), new RockJob("asteroid_l4", "m2",     -60, 140,  20, 512),
        new RockJob("asteroid_m1", "m3.001",   0,   0,   0, 384), new RockJob("asteroid_m2", "m3",       0,  90,   0, 384),
        new RockJob("asteroid_m3", "m3.001",  80,  40,  30, 384), new RockJob("asteroid_m4", "m2",      40, -60, 150, 384),
        new RockJob("asteroid_s1", "m3.002",   0,  90,   0, 256), new RockJob("asteroid_s2", "m3.002",  60,  20,  45, 256),
        new RockJob("asteroid_s3", "m3",      50, 200,  10, 256), new RockJob("asteroid_s4", "m3.001", -40, 110,  70, 256),
    };

    [MenuItem("Filip/Bake Rocks And Stars From FBX")]
    static void BakeMenu()
    {
        if (!EditorUtility.DisplayDialog("Bake rocks and stars",
            "This overwrites asteroid_*.png, star*.png and star_glow*.png in Assets/Resources/Art " +
            "with pictures rendered from Assets/Art/Source/stone.fbx and stars.fbx.\n\nContinue?", "Bake", "Cancel"))
            return;
        BakeAll();
    }

    // Also usable from batch mode: Unity -batchmode -quit -executeMethod ArtBake.BakeAll
    public static void BakeAll()
    {
        var written = new List<string>();
        try
        {
            // Make sure the models use the import settings from ArtImportSettings.
            foreach (var p in new[] { StoneModel, StarsModel })
                if (File.Exists(p)) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            if (File.Exists(StoneModel))
            {
                var meshes = MeshesByName(StoneModel);
                for (int i = 0; i < RockJobs.Length; i++)
                {
                    var j = RockJobs[i];
                    EditorUtility.DisplayProgressBar("Bake rocks", j.name, (float)i / RockJobs.Length);
                    Mesh mesh;
                    if (!meshes.TryGetValue(j.model, out mesh))
                    {
                        Debug.LogError("ArtBake: model '" + j.model + "' not found in " + StoneModel +
                            ". Models in the file: " + string.Join(", ", meshes.Keys));
                        continue;
                    }
                    string path = OutFolder + j.name + ".png";
                    File.WriteAllBytes(path, BakeRock(mesh, j.rx, j.ry, j.rz, j.size));
                    written.Add(path);
                }
            }
            else Debug.LogWarning("ArtBake: " + StoneModel + " not found, rocks skipped.");

            if (File.Exists(StarsModel))
            {
                var meshes = MeshesByName(StarsModel);
                int n = 0;
                foreach (var kv in meshes)
                {
                    EditorUtility.DisplayProgressBar("Bake stars", kv.Key, (float)n++ / meshes.Count);
                    string name = kv.Key;                                   // star1, star2, ...
                    string digits = name.TrimStart("abcdefghijklmnopqrstuvwxyz_".ToCharArray());
                    string prefix = name.Substring(0, name.Length - digits.Length);
                    byte[] core, glow;
                    BakeStar(kv.Value, StarSize, out core, out glow);
                    string corePath = OutFolder + name + ".png";
                    string glowPath = OutFolder + prefix + "_glow" + digits + ".png";
                    File.WriteAllBytes(corePath, core);
                    File.WriteAllBytes(glowPath, glow);
                    written.Add(corePath);
                    written.Add(glowPath);
                }
            }
            else Debug.LogWarning("ArtBake: " + StarsModel + " not found, stars skipped.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        foreach (var p in written) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        Debug.Log("ArtBake: wrote " + written.Count + " pictures:\n" + string.Join("\n", written));
    }

    // Every mesh in the model file, by object name (as named in Blender).
    static Dictionary<string, Mesh> MeshesByName(string path)
    {
        var result = new Dictionary<string, Mesh>();
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) return result;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            Mesh m = null;
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null) m = mf.sharedMesh;
            var smr = t.GetComponent<SkinnedMeshRenderer>();
            if (m == null && smr != null) m = smr.sharedMesh;
            if (m != null && !result.ContainsKey(t.name)) result[t.name] = m;
        }
        return result;
    }

    // Unity mirrors X when it imports an FBX file; undo that so views match the file (and the old pictures).
    static double[] FileSpace(Vector3 v) { return new double[] { -v.x, v.y, v.z }; }

    // ---------------- Rocks ----------------

    class Tri { public double[][] p = new double[3][], n = new double[3][], o = new double[3][]; }

    static double[] Rot(double[] m, double x, double y, double z)
    {
        return new double[] { m[0] * x + m[1] * y + m[2] * z, m[3] * x + m[4] * y + m[5] * z, m[6] * x + m[7] * y + m[8] * z };
    }

    // Rz * Ry * Rx
    static double[] Euler(double rx, double ry, double rz)
    {
        double a = rx * Math.PI / 180, b = ry * Math.PI / 180, c = rz * Math.PI / 180;
        double ca = Math.Cos(a), sa = Math.Sin(a), cb = Math.Cos(b), sb = Math.Sin(b), cc = Math.Cos(c), sc = Math.Sin(c);
        return new double[] {
            cc * cb, cc * sb * sa - sc * ca, cc * sb * ca + sc * sa,
            sc * cb, sc * sb * sa + cc * ca, sc * sb * ca - cc * sa,
            -sb, cb * sa, cb * ca };
    }

    static List<Tri> Triangles(Mesh mesh, double[] view)
    {
        var verts = mesh.vertices;
        var normals = mesh.normals;
        var idx = mesh.triangles;
        bool hasNormals = normals != null && normals.Length == verts.Length;
        var tris = new List<Tri>(idx.Length / 3);
        for (int k = 0; k + 2 < idx.Length; k += 3)
        {
            var tri = new Tri();
            for (int c = 0; c < 3; c++)
            {
                int vi = idx[k + c];
                tri.o[c] = FileSpace(verts[vi]);
                tri.p[c] = Rot(view, tri.o[c][0], tri.o[c][1], tri.o[c][2]);
                if (hasNormals)
                {
                    var n = FileSpace(normals[vi]);
                    tri.n[c] = Rot(view, n[0], n[1], n[2]);
                }
            }
            if (!hasNormals)
            {
                double[] e1 = Sub(tri.p[1], tri.p[0]), e2 = Sub(tri.p[2], tri.p[0]);
                double[] fn = Norm(new double[] { e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0] });
                tri.n[0] = fn; tri.n[1] = fn; tri.n[2] = fn;
            }
            tris.Add(tri);
        }
        return tris;
    }

    static double[] Sub(double[] a, double[] b) { return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
    static double[] Norm(double[] a)
    {
        double l = Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]); if (l < 1e-12) return new double[] { 0, 0, 1 };
        return new double[] { a[0] / l, a[1] / l, a[2] / l };
    }
    static float C01(float t) { return t < 0f ? 0f : (t > 1f ? 1f : t); }

    // Cheap 3D value noise, 0..1.
    static float Hash(int x, int y, int z)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + z * 2147483647u.GetHashCode());
            h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
    static float Noise3(double x, double y, double z)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y), zi = (int)Math.Floor(z);
        float fx = (float)(x - xi), fy = (float)(y - yi), fz = (float)(z - zi);
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz);
        float a = 0f;
        for (int dz = 0; dz < 2; dz++) for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++)
            a += Hash(xi + dx, yi + dy, zi + dz) * (dx == 0 ? 1 - fx : fx) * (dy == 0 ? 1 - fy : fy) * (dz == 0 ? 1 - fz : fz);
        return a;
    }

    static byte[] BakeRock(Mesh mesh, double rx, double ry, double rz, int size)
    {
        double[] view = Euler(rx, ry, rz);
        var tris = Triangles(mesh, view);

        // Pass 1: coarse silhouette -> area, centroid, extents.
        double minX = 1e30, maxX = -1e30, minY = 1e30, maxY = -1e30;
        foreach (var t in tris) for (int c = 0; c < 3; c++)
        {
            minX = Math.Min(minX, t.p[c][0]); maxX = Math.Max(maxX, t.p[c][0]);
            minY = Math.Min(minY, t.p[c][1]); maxY = Math.Max(maxY, t.p[c][1]);
        }
        double span3 = Math.Max(maxX - minX, maxY - minY);
        int G = 300; double gs = (G - 4) / span3;
        bool[] cov = new bool[G * G];
        foreach (var t in tris) Raster(t, G, gs, minX, minY, null, null, null, cov, 2);
        double area = 0, sx = 0, sy = 0;
        for (int y = 0; y < G; y++) for (int x = 0; x < G; x++) if (cov[y * G + x]) { area++; sx += x; sy += y; }
        double cx = minX + (sx / area - 2) / gs, cy = minY + (sy / area - 2) / gs;
        double equivR = Math.Sqrt(area / Math.PI) / gs;

        // Pass 2: full render, 3x supersampled.
        int SS = 3, W = size * SS;
        double scale = 0.45 * W / equivR;
        double reach = Math.Max(Math.Max(maxX - cx, cx - minX), Math.Max(maxY - cy, cy - minY));
        if (reach * scale > 0.49 * W) scale = 0.49 * W / reach;
        float[] zb = new float[W * W]; for (int i = 0; i < zb.Length; i++) zb[i] = float.NegativeInfinity;
        float[] nb = new float[W * W * 3], ob = new float[W * W * 3];
        double ox = cx - (W / 2.0) / scale, oy = cy - (W / 2.0) / scale;
        foreach (var t in tris) Raster(t, W, scale, ox, oy, zb, nb, ob, null, 0);

        // Shade.
        float[] rgb = new float[W * W * 3];
        double freq = 7.0 / span3;
        float[] L = { -0.42f, 0.52f, 0.74f };
        int aoR1 = Math.Max(2, W / 45), aoR2 = Math.Max(4, W / 18); float aoRange = W * 0.07f;
        for (int y = 0; y < W; y++) for (int x = 0; x < W; x++)
        {
            int i = y * W + x; if (float.IsNegativeInfinity(zb[i])) continue;
            float nx = nb[i * 3], ny = nb[i * 3 + 1], nz = nb[i * 3 + 2];
            float nl = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz); if (nl < 1e-6f) nl = 1f; nx /= nl; ny /= nl; nz /= nl;
            float nz0 = Math.Max(0f, nz);              // smooth normal, kept for a clean rim
            // Rocky micro-relief: tilt the normal by the slope of a 3D noise (two sizes of bumps).
            double qx = ob[i * 3] * freq, qy = ob[i * 3 + 1] * freq, qz = ob[i * 3 + 2] * freq;
            float bx = 0f, by = 0f, bz = 0f;
            for (int oct = 0; oct < 2; oct++)
            {
                double f = oct == 0 ? 2.2 : 6.5, e = 0.08; float w = oct == 0 ? 0.55f : 0.30f;
                float n0 = Noise3(qx * f, qy * f, qz * f);
                bx += w * (Noise3(qx * f + e, qy * f, qz * f) - n0) / (float)e;
                by += w * (Noise3(qx * f, qy * f + e, qz * f) - n0) / (float)e;
                bz += w * (Noise3(qx * f, qy * f, qz * f + e) - n0) / (float)e;
            }
            double[] gv = Rot(view, bx, by, bz);       // bump slope, turned to the camera like the model
            float gd = (float)(gv[0] * nx + gv[1] * ny + gv[2] * nz);
            nx -= 0.55f * ((float)gv[0] - gd * nx); ny -= 0.55f * ((float)gv[1] - gd * ny); nz -= 0.55f * ((float)gv[2] - gd * nz);
            nl = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz); if (nl < 1e-6f) nl = 1f; nx /= nl; ny /= nl; nz /= nl;

            float ndv = Math.Max(0f, nz), ndl = Math.Max(0f, nx * L[0] + ny * L[1] + nz * L[2]);

            // Ambient occlusion from the depth picture: darker where neighbours stick out in front.
            float occ = 0f; int cnt = 0;
            for (int s = 0; s < 16; s++)
            {
                double ang = s * Math.PI * 2 / 8 + (s >= 8 ? 0.39 : 0); int rad = s < 8 ? aoR1 : aoR2;
                int sx2 = x + (int)(Math.Cos(ang) * rad), sy2 = y + (int)(Math.Sin(ang) * rad);
                if (sx2 < 0 || sy2 < 0 || sx2 >= W || sy2 >= W) continue;
                float sz = zb[sy2 * W + sx2]; cnt++;
                if (float.IsNegativeInfinity(sz)) continue;
                occ += C01((sz - zb[i]) / aoRange);
            }
            float ao = 1f - 1.5f * (cnt > 0 ? occ / cnt : 0f); if (ao < 0.2f) ao = 0.2f;

            float grain = 0.6f * Noise3(qx, qy, qz) + 0.4f * Noise3(qx * 3.1, qy * 3.1, qz * 3.1);
            float patch = Noise3(qx * 0.45 + 31.7, qy * 0.45, qz * 0.45);     // big warm / cool patches
            float alb = 0.69f * (0.80f + 0.40f * grain);
            float diff = (0.16f + 0.34f * ndv + 0.78f * ndl) * ao;
            float rim = (float)Math.Pow(1f - nz0, 3.2) * 0.55f;
            // Warm light clay (stands out against the cool nebula), with a light warm rim.
            float wr = 1.07f - 0.07f * patch, wb = 0.82f + 0.10f * patch;
            rgb[i * 3] = C01(alb * wr * diff + rim * 1.00f);
            rgb[i * 3 + 1] = C01(alb * 0.98f * diff + rim * 0.90f);
            rgb[i * 3 + 2] = C01(alb * wb * diff + rim * 0.72f);
        }

        // Downsample to the final size. The buffers are top-down; Texture2D rows go bottom-up.
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float r = 0, g = 0, b = 0; int n = 0;
            for (int dy = 0; dy < SS; dy++) for (int dx = 0; dx < SS; dx++)
            {
                int i = (y * SS + dy) * W + (x * SS + dx); if (float.IsNegativeInfinity(zb[i])) continue;
                r += rgb[i * 3]; g += rgb[i * 3 + 1]; b += rgb[i * 3 + 2]; n++;
            }
            int o = (size - 1 - y) * size + x;
            px[o] = n == 0 ? new Color32(0, 0, 0, 0)
                : new Color32((byte)(r / n * 255f), (byte)(g / n * 255f), (byte)(b / n * 255f), (byte)(255f * n / (SS * SS)));
        }
        return EncodePng(px, size);
    }

    // Draws one triangle. Picture y grows downward; model y grows upward.
    static void Raster(Tri t, int W, double scale, double ox, double oy, float[] zb, float[] nb, float[] ob, bool[] cov, int pad)
    {
        double[] X = new double[3], Y = new double[3];
        for (int c = 0; c < 3; c++) { X[c] = (t.p[c][0] - ox) * scale + pad; Y[c] = W - ((t.p[c][1] - oy) * scale + pad); }
        int x0 = Math.Max(0, (int)Math.Floor(Math.Min(X[0], Math.Min(X[1], X[2])))), x1 = Math.Min(W - 1, (int)Math.Ceiling(Math.Max(X[0], Math.Max(X[1], X[2]))));
        int y0 = Math.Max(0, (int)Math.Floor(Math.Min(Y[0], Math.Min(Y[1], Y[2])))), y1 = Math.Min(W - 1, (int)Math.Ceiling(Math.Max(Y[0], Math.Max(Y[1], Y[2]))));
        double den = (Y[1] - Y[2]) * (X[0] - X[2]) + (X[2] - X[1]) * (Y[0] - Y[2]);
        if (Math.Abs(den) < 1e-12) return;
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
        {
            double px = x + 0.5, py = y + 0.5;
            double a = ((Y[1] - Y[2]) * (px - X[2]) + (X[2] - X[1]) * (py - Y[2])) / den;
            double b = ((Y[2] - Y[0]) * (px - X[2]) + (X[0] - X[2]) * (py - Y[2])) / den;
            double c = 1 - a - b;
            if (a < -1e-9 || b < -1e-9 || c < -1e-9) continue;
            int i = y * W + x;
            if (cov != null) { cov[i] = true; continue; }
            float z = (float)((a * t.p[0][2] + b * t.p[1][2] + c * t.p[2][2]) * scale);
            if (z <= zb[i]) continue;
            zb[i] = z;
            for (int k = 0; k < 3; k++)
            {
                nb[i * 3 + k] = (float)(a * t.n[0][k] + b * t.n[1][k] + c * t.n[2][k]);
                ob[i * 3 + k] = (float)(a * t.o[0][k] + b * t.o[1][k] + c * t.o[2][k]);
            }
        }
    }

    // ---------------- Stars ----------------

    // The star model is flat: it is drawn onto its two widest axes as a white shape.
    static void BakeStar(Mesh mesh, int size, out byte[] corePng, out byte[] glowPng)
    {
        var verts = mesh.vertices;
        var idx = mesh.triangles;
        var v = new double[verts.Length][];
        double[] mn = { 1e30, 1e30, 1e30 }, mx = { -1e30, -1e30, -1e30 };
        for (int i = 0; i < verts.Length; i++)
        {
            v[i] = FileSpace(verts[i]);
            for (int k = 0; k < 3; k++) { if (v[i][k] < mn[k]) mn[k] = v[i][k]; if (v[i][k] > mx[k]) mx[k] = v[i][k]; }
        }
        int thin = 0; for (int k = 1; k < 3; k++) if (mx[k] - mn[k] < mx[thin] - mn[thin]) thin = k;
        int ax = thin == 0 ? 1 : 0, ay = thin == 2 ? 1 : 2;
        double span = Math.Max(mx[ax] - mn[ax], mx[ay] - mn[ay]);
        double cx = (mn[ax] + mx[ax]) / 2, cy = (mn[ay] + mx[ay]) / 2;

        // Sharp shape, drawn 4x bigger and then scaled down. The shape takes only 42% of the
        // picture: the rest is room for the glow. Rows are bottom-up here (like Texture2D).
        int SS = 4, big = size * SS;
        double sc = big * 0.42 / span;
        var hi = new bool[big * big];
        for (int k = 0; k + 2 < idx.Length; k += 3)
        {
            double[] X = new double[3], Y = new double[3];
            for (int c = 0; c < 3; c++)
            {
                var p = v[idx[k + c]];
                X[c] = big / 2.0 + (p[ax] - cx) * sc;
                Y[c] = big / 2.0 + (p[ay] - cy) * sc;
            }
            FillTriangle(hi, big, X, Y);
        }

        float[] al = new float[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            int n = 0;
            for (int dy = 0; dy < SS; dy++) for (int dx = 0; dx < SS; dx++) if (hi[(y * SS + dy) * big + x * SS + dx]) n++;
            al[y * size + x] = n / (float)(SS * SS);
        }

        // Glow: the shape blurred at three sizes (tight, medium, wide) plus a round aura, fading out to the edges.
        float[] g1 = Blur(al, size, size / 64), g2 = Blur(al, size, size / 20), g3 = Blur(al, size, size / 8);
        var core = new Color32[size * size];
        var glowPx = new Color32[size * size];
        float half = size / 2f;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            int i = y * size + x;
            float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            float aura = 0.42f * (float)Math.Exp(-(d * d) / (0.34f * 0.34f));
            float glow = 1f - (1f - C01(g1[i] * 0.9f)) * (1f - C01(g2[i] * 2.2f)) * (1f - C01(g3[i] * 4.0f)) * (1f - aura);
            float window = 1f - C01((d - 0.70f) / 0.30f); window = window * window * (3f - 2f * window);
            glow *= window;
            glowPx[i] = new Color32(255, 255, 255, (byte)(C01(glow) * 255f));
            core[i] = new Color32(255, 255, 255, (byte)(al[i] * 255f));
        }
        corePng = EncodePng(core, size);
        glowPng = EncodePng(glowPx, size);
    }

    // Fills pixels whose centers are inside the triangle (edges included, so neighbours leave no seams).
    static void FillTriangle(bool[] buf, int W, double[] X, double[] Y)
    {
        int x0 = Math.Max(0, (int)Math.Floor(Math.Min(X[0], Math.Min(X[1], X[2])))), x1 = Math.Min(W - 1, (int)Math.Ceiling(Math.Max(X[0], Math.Max(X[1], X[2]))));
        int y0 = Math.Max(0, (int)Math.Floor(Math.Min(Y[0], Math.Min(Y[1], Y[2])))), y1 = Math.Min(W - 1, (int)Math.Ceiling(Math.Max(Y[0], Math.Max(Y[1], Y[2]))));
        double den = (Y[1] - Y[2]) * (X[0] - X[2]) + (X[2] - X[1]) * (Y[0] - Y[2]);
        if (Math.Abs(den) < 1e-12) return;
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
        {
            double px = x + 0.5, py = y + 0.5;
            double a = ((Y[1] - Y[2]) * (px - X[2]) + (X[2] - X[1]) * (py - Y[2])) / den;
            double b = ((Y[2] - Y[0]) * (px - X[2]) + (X[0] - X[2]) * (py - Y[2])) / den;
            if (a < -1e-6 || b < -1e-6 || 1 - a - b < -1e-6) continue;
            buf[y * W + x] = true;
        }
    }

    // Soft blur: three box blurs in a row are very close to a Gaussian.
    static float[] Blur(float[] src, int n, int radius)
    {
        float[] a = (float[])src.Clone(), t = new float[n * n];
        for (int pass = 0; pass < 3; pass++)
        {
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float s = 0f; for (int k = -radius; k <= radius; k++) { int xx = x + k; if (xx >= 0 && xx < n) s += a[y * n + xx]; }
                t[y * n + x] = s / (2 * radius + 1);
            }
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float s = 0f; for (int k = -radius; k <= radius; k++) { int yy = y + k; if (yy >= 0 && yy < n) s += t[yy * n + x]; }
                a[y * n + x] = s / (2 * radius + 1);
            }
        }
        return a;
    }

    static byte[] EncodePng(Color32[] px, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        var png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        return png;
    }
}
