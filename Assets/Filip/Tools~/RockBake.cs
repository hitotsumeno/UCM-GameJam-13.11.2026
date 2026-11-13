using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

// Renders a 3D model from an FBX file into a shaded 2D picture with transparency.
// Lighting is mostly "from the camera", so the picture still looks right when the
// game spins it. A cool rim light ties the rock to the nebula background.
// The rock is scaled so that a circle of 0.45 x picture size has the same area as
// the rock: the game uses exactly that circle for collisions.
public static class RockBake
{
    class Tri { public double[][] p = new double[3][], n = new double[3][], o = new double[3][]; }

    static FNode LoadRoot(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var r = new BinaryReader(new MemoryStream(bytes));
        r.BaseStream.Position = 27;
        var root = new FNode();
        while (r.BaseStream.Position < bytes.Length - 13)
        {
            var n = FbxDump.ReadNode(r, bytes.Length);
            if (n == null) break;
            root.Children.Add(n);
        }
        return root;
    }

    static string Clean(string s) { int i = s.IndexOf('\0'); return i >= 0 ? s.Substring(0, i) : s; }

    static FNode FindGeometry(FNode root, string modelName)
    {
        long modelId = 0; var geos = new Dictionary<long, FNode>();
        foreach (var o in root.Find("Objects").Children)
        {
            if (o.Name == "Model" && Clean((string)o.Props[1]) == modelName) modelId = (long)o.Props[0];
            if (o.Name == "Geometry") geos[(long)o.Props[0]] = o;
        }
        foreach (var c in root.Find("Connections").Children)
        {
            long a = (long)c.Props[1], b = (long)c.Props[2];
            if (b == modelId && geos.ContainsKey(a)) return geos[a];
        }
        throw new Exception("Model not found: " + modelName);
    }

    static double[] Rot(double[] m, double x, double y, double z)
    {
        return new double[] { m[0] * x + m[1] * y + m[2] * z, m[3] * x + m[4] * y + m[5] * z, m[6] * x + m[7] * y + m[8] * z };
    }

    static double[] Euler(double rx, double ry, double rz)
    {
        double a = rx * Math.PI / 180, b = ry * Math.PI / 180, c = rz * Math.PI / 180;
        double ca = Math.Cos(a), sa = Math.Sin(a), cb = Math.Cos(b), sb = Math.Sin(b), cc = Math.Cos(c), sc = Math.Sin(c);
        // Rz * Ry * Rx
        return new double[] {
            cc * cb, cc * sb * sa - sc * ca, cc * sb * ca + sc * sa,
            sc * cb, sc * sb * sa + cc * ca, sc * sb * ca - cc * sa,
            -sb, cb * sa, cb * ca };
    }

    static List<Tri> Triangles(FNode geo, double[] m)
    {
        double[] v = (double[])geo.Find("Vertices").Props[0];
        int[] idx = (int[])geo.Find("PolygonVertexIndex").Props[0];
        var ln = geo.Find("LayerElementNormal");
        double[] nrm = null; int[] nidx = null; bool byPolyVertex = true;
        if (ln != null && ln.Find("Normals") != null)
        {
            nrm = (double[])ln.Find("Normals").Props[0];
            byPolyVertex = ((string)ln.Find("MappingInformationType").Props[0]).StartsWith("ByPolygonVertex");
            if (ln.Find("NormalsIndex") != null && ((string)ln.Find("ReferenceInformationType").Props[0]) != "Direct")
                nidx = (int[])ln.Find("NormalsIndex").Props[0];
        }
        var tris = new List<Tri>();
        var poly = new List<int>(); var polyK = new List<int>();
        for (int k = 0; k < idx.Length; k++)
        {
            int raw = idx[k]; int vi = raw < 0 ? ~raw : raw;
            poly.Add(vi); polyK.Add(k);
            if (raw >= 0) continue;
            for (int t = 1; t + 1 < poly.Count; t++)
            {
                var tri = new Tri(); int[] corners = { 0, t, t + 1 };
                for (int c = 0; c < 3; c++)
                {
                    int pv = poly[corners[c]], pk = polyK[corners[c]];
                    tri.o[c] = new double[] { v[pv * 3], v[pv * 3 + 1], v[pv * 3 + 2] };
                    tri.p[c] = Rot(m, tri.o[c][0], tri.o[c][1], tri.o[c][2]);
                    if (nrm != null)
                    {
                        int ni = byPolyVertex ? pk : pv; if (nidx != null) ni = nidx[ni];
                        tri.n[c] = Rot(m, nrm[ni * 3], nrm[ni * 3 + 1], nrm[ni * 3 + 2]);
                    }
                }
                if (nrm == null)
                {
                    double[] e1 = Sub(tri.p[1], tri.p[0]), e2 = Sub(tri.p[2], tri.p[0]);
                    double[] fn = Norm(new double[] { e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0] });
                    tri.n[0] = fn; tri.n[1] = fn; tri.n[2] = fn;
                }
                tris.Add(tri);
            }
            poly.Clear(); polyK.Clear();
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

    public static string Bake(string fbx, string model, double rx, double ry, double rz, int size, string outFile)
    {
        var geo = FindGeometry(LoadRoot(fbx), model);
        double[] view = Euler(rx, ry, rz);
        var tris = Triangles(geo, view);

        // Pass 1: coarse silhouette -> area, centroid, extents.
        double minX = 1e30, maxX = -1e30, minY = 1e30, maxY = -1e30, span3 = 0;
        foreach (var t in tris) for (int c = 0; c < 3; c++)
        {
            minX = Math.Min(minX, t.p[c][0]); maxX = Math.Max(maxX, t.p[c][0]);
            minY = Math.Min(minY, t.p[c][1]); maxY = Math.Max(maxY, t.p[c][1]);
        }
        span3 = Math.Max(maxX - minX, maxY - minY);
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

        // Downsample to the final size.
        byte[] px = new byte[size * size * 4];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float r = 0, g = 0, b = 0; int n = 0;
            for (int dy = 0; dy < SS; dy++) for (int dx = 0; dx < SS; dx++)
            {
                int i = (y * SS + dy) * W + (x * SS + dx); if (float.IsNegativeInfinity(zb[i])) continue;
                r += rgb[i * 3]; g += rgb[i * 3 + 1]; b += rgb[i * 3 + 2]; n++;
            }
            int o = (y * size + x) * 4;
            if (n == 0) { px[o + 3] = 0; continue; }
            px[o] = (byte)(b / n * 255f); px[o + 1] = (byte)(g / n * 255f); px[o + 2] = (byte)(r / n * 255f);
            px[o + 3] = (byte)(255f * n / (SS * SS));
        }
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data); bmp.Save(outFile, ImageFormat.Png); bmp.Dispose();
        return Path.GetFileName(outFile) + "  <- " + model + " (" + rx + "," + ry + "," + rz + "), " + tris.Count + " triangles, fill " +
            (reach * scale / (0.5 * W)).ToString("0.00");
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

    // Test picture: the given sprites drawn over a background, sizes in screen pixels.
    public static void Sheet(string background, string[] files, int[] sizes, string outPath)
    {
        var bg = new Bitmap(background);
        var bmp = new Bitmap(bg.Width, bg.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.DrawImage(bg, 0, 0, bg.Width, bg.Height);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            int x = 20, y = 20, rowH = 0;
            for (int i = 0; i < files.Length; i++)
            {
                if (x + sizes[i] > bg.Width - 10) { x = 20; y += rowH + 16; rowH = 0; }
                using (var img = new Bitmap(files[i])) g.DrawImage(img, x, y, sizes[i], sizes[i]);
                x += sizes[i] + 16; rowH = Math.Max(rowH, sizes[i]);
            }
        }
        bmp.Save(outPath, ImageFormat.Png);
    }
}
