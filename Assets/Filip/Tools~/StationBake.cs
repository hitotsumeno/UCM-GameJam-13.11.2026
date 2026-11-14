using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// The wreck of a ring station for the background of level 2.
// It is built in 3D from simple solids (cylinders, boxes, pieces of a ring) and
// rendered with the same renderer as the rocks, so it has real volume and light.
public static class StationBake
{
    static List<RockBake.Tri> tris;
    static double[] view;
    static Random rng;

    // Materials: 0 hull, 1 solar cell, 2 dark structure, 3 light that is still on.
    static double[] V(double x, double y, double z) { return new[] { x, y, z }; }
    static double[] Add(double[] a, double[] b) { return V(a[0] + b[0], a[1] + b[1], a[2] + b[2]); }
    static double[] Mul(double[] a, double s) { return V(a[0] * s, a[1] * s, a[2] * s); }
    static double[] Cross(double[] a, double[] b) { return V(a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]); }

    static void Tri(double[] a, double[] b, double[] c, double[] na, double[] nb, double[] nc, int mat)
    {
        var t = new RockBake.Tri(); t.mat = mat;
        double[][] pos = { a, b, c }, nrm = { na, nb, nc };
        for (int i = 0; i < 3; i++)
        {
            t.o[i] = pos[i];
            t.p[i] = RockBake.Rot(view, pos[i][0], pos[i][1], pos[i][2]);
            t.n[i] = RockBake.Rot(view, nrm[i][0], nrm[i][1], nrm[i][2]);
        }
        tris.Add(t);
    }

    static void Quad(double[] a, double[] b, double[] c, double[] d, double[] na, double[] nb, double[] nc, double[] nd, int mat)
    {
        Tri(a, b, c, na, nb, nc, mat); Tri(a, c, d, na, nc, nd, mat);
    }

    // Box: center, half sizes, turn angles in degrees.
    static void Box(double[] c, double hx, double hy, double hz, double rx, double ry, double rz, int mat)
    {
        double[] m = RockBake.Euler(rx, ry, rz);
        double[][] axes = { RockBake.Rot(m, 1, 0, 0), RockBake.Rot(m, 0, 1, 0), RockBake.Rot(m, 0, 0, 1) };
        double[] h = { hx, hy, hz };
        for (int a = 0; a < 3; a++) for (int s = -1; s <= 1; s += 2)
        {
            int b = (a + 1) % 3, d = (a + 2) % 3;
            double[] n = Mul(axes[a], s), fc = Add(c, Mul(axes[a], s * h[a]));
            double[] ub = Mul(axes[b], h[b]), ud = Mul(axes[d], h[d]);
            Quad(Add(Add(fc, ub), ud), Add(Add(fc, Mul(ub, -1)), ud), Add(Add(fc, Mul(ub, -1)), Mul(ud, -1)), Add(Add(fc, ub), Mul(ud, -1)), n, n, n, n, mat);
        }
    }

    static void Cylinder(double[] p0, double[] p1, double r, int seg, int mat)
    {
        double[] axis = RockBake.Norm(RockBake.Sub(p1, p0));
        double[] u = Cross(axis, V(0, 0, 1));
        if (u[0] * u[0] + u[1] * u[1] + u[2] * u[2] < 1e-6) u = Cross(axis, V(0, 1, 0));
        u = RockBake.Norm(u); double[] w = Cross(axis, u);
        for (int i = 0; i < seg; i++)
        {
            double a0 = Math.PI * 2 * i / seg, a1 = Math.PI * 2 * (i + 1) / seg;
            double[] n0 = Add(Mul(u, Math.Cos(a0)), Mul(w, Math.Sin(a0))), n1 = Add(Mul(u, Math.Cos(a1)), Mul(w, Math.Sin(a1)));
            Quad(Add(p0, Mul(n0, r)), Add(p1, Mul(n0, r)), Add(p1, Mul(n1, r)), Add(p0, Mul(n1, r)), n0, n0, n1, n1, mat);
            double[] back = Mul(axis, -1);
            Tri(p0, Add(p0, Mul(n1, r)), Add(p0, Mul(n0, r)), back, back, back, mat);
            Tri(p1, Add(p1, Mul(n0, r)), Add(p1, Mul(n1, r)), axis, axis, axis, mat);
        }
    }

    static void Sphere(double[] c, double r, int mat)
    {
        int su = 16, sv = 10;
        for (int i = 0; i < su; i++) for (int j = 0; j < sv; j++)
        {
            double[][] n = new double[4][]; int k = 0;
            foreach (int[] d in new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 0, 1 } })
            {
                double a = Math.PI * 2 * (i + d[0]) / su, b = Math.PI * (j + d[1]) / sv;
                n[k++] = V(Math.Sin(b) * Math.Cos(a), Math.Sin(b) * Math.Sin(a), Math.Cos(b));
            }
            Quad(Add(c, Mul(n[0], r)), Add(c, Mul(n[1], r)), Add(c, Mul(n[2], r)), Add(c, Mul(n[3], r)), n[0], n[1], n[2], n[3], mat);
        }
    }

    // A piece of a ring lying in the XY plane around the origin. R: ring radius, r: tube radius.
    static void RingArc(double R, double r, double a0deg, double a1deg, int mat)
    {
        int su = Math.Max(4, (int)((a1deg - a0deg) / 4)), sv = 14;
        for (int i = 0; i < su; i++) for (int j = 0; j < sv; j++)
        {
            double[][] pos = new double[4][], nrm = new double[4][]; int k = 0;
            foreach (int[] d in new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 0, 1 } })
            {
                double u = (a0deg + (a1deg - a0deg) * (i + d[0]) / su) * Math.PI / 180, w = Math.PI * 2 * (j + d[1]) / sv;
                double[] radial = V(Math.Cos(u), Math.Sin(u), 0);
                nrm[k] = Add(Mul(radial, Math.Cos(w)), V(0, 0, Math.Sin(w)));
                pos[k] = Add(Mul(radial, R), Mul(nrm[k], r)); k++;
            }
            Quad(pos[0], pos[1], pos[2], pos[3], nrm[0], nrm[1], nrm[2], nrm[3], mat);
        }
    }

    // Lattice beam from a to b: three rails and zigzag braces.
    static void Truss(double[] a, double[] b, double width)
    {
        double[] axis = RockBake.Norm(RockBake.Sub(b, a)), side = RockBake.Norm(Cross(axis, V(0, 0, 1))), up = V(0, 0, 1);
        double len = Math.Sqrt(Math.Pow(b[0] - a[0], 2) + Math.Pow(b[1] - a[1], 2) + Math.Pow(b[2] - a[2], 2));
        double[][] rails = { Mul(side, width), Mul(side, -width), Mul(up, width * 1.3) };
        foreach (var off in rails) Cylinder(Add(a, off), Add(b, off), 0.09, 6, 2);
        int n = Math.Max(2, (int)(len / (width * 2.2)));
        for (int i = 0; i < n; i++)
        {
            double[] p0 = Add(a, Mul(axis, len * i / n)), p1 = Add(a, Mul(axis, len * (i + 1) / n));
            Cylinder(Add(p0, rails[i % 2]), Add(p1, rails[(i + 1) % 2]), 0.06, 5, 2);
            Cylinder(Add(p0, rails[2]), Add(p1, rails[i % 2]), 0.06, 5, 2);
        }
    }

    static void Build()
    {
        tris = new List<RockBake.Tri>(); rng = new Random(12);
        view = RockBake.Euler(-56, 0, 22);   // tilt the ring so it reads as an ellipse

        // Hub with two modules on its axis.
        Cylinder(V(0, 0, -3), V(0, 0, 3), 2.4, 28, 0);
        Cylinder(V(0, 0, 3), V(0, 0, 5.6), 1.5, 22, 0);
        Sphere(V(0, 0, 5.6), 1.5, 0);
        Cylinder(V(0, 0, -3), V(0, 0, -4.4), 1.7, 22, 2);

        // The ring is broken in three places.
        RingArc(10, 1.05, -14, 104, 0);
        RingArc(10, 1.05, 130, 214, 0);
        RingArc(10, 1.05, 250, 322, 0);

        // Spokes; one is snapped half-way.
        foreach (double ang in new[] { 35.0, 155.0, 285.0 })
        {
            double[] d = V(Math.Cos(ang * Math.PI / 180), Math.Sin(ang * Math.PI / 180), 0);
            Cylinder(Mul(d, 2.3), Mul(d, 9.1), 0.34, 10, 2);
        }
        double[] broken = V(Math.Cos(205 * Math.PI / 180), Math.Sin(205 * Math.PI / 180), 0);
        Cylinder(Mul(broken, 2.3), Mul(broken, 5.6), 0.34, 10, 2);

        // Right arm: lattice beam with a solar array that survived (a few cells are gone).
        Truss(V(2.4, 0, 0), V(19.5, 0, 0), 0.45);
        for (int i = 0; i < 5; i++) for (int j = -1; j <= 1; j += 2)
        {
            if ((i == 1 && j == 1) || (i == 3 && j == -1)) continue;
            Box(V(13.2 + i * 1.45, j * 2.0, 0), 0.62, 1.35, 0.05, 0, 0, 0, 1);
        }

        // Left arm: snapped beam, its array torn off and drifting away.
        Truss(V(-2.4, 0, 0), V(-8.2, 0, 0), 0.45);
        for (int i = 0; i < 7; i++)
            Box(V(-12 - rng.NextDouble() * 9, (rng.NextDouble() - 0.5) * 9, (rng.NextDouble() - 0.5) * 5),
                0.62, 1.35, 0.05, rng.NextDouble() * 360, rng.NextDouble() * 360, rng.NextDouble() * 360, 1);
        Truss(V(-11.5, 2.6, 1.2), V(-15.5, 4.4, -0.6), 0.45);

        // Loose debris around.
        for (int i = 0; i < 46; i++)
        {
            double a = rng.NextDouble() * Math.PI * 2, d = 5 + rng.NextDouble() * 16, s = 0.12 + rng.NextDouble() * 0.38;
            Box(V(Math.Cos(a) * d, Math.Sin(a) * d, (rng.NextDouble() - 0.5) * 7), s, s * (0.4 + rng.NextDouble()), s * 0.5,
                rng.NextDouble() * 360, rng.NextDouble() * 360, rng.NextDouble() * 360, rng.Next(3) == 0 ? 0 : 2);
        }

        // A few lights are still on.
        Sphere(V(2.45, 0.4, 1.2), 0.2, 3); Sphere(V(-1.2, 2.2, -1.0), 0.2, 3);
        Sphere(V(10.9, 2.2, 0.6), 0.22, 3); Sphere(V(-7.0, -8.4, 0.3), 0.22, 3); Sphere(V(0, 0, 7.2), 0.18, 3);
    }

    // Soft blur of a picture with transparency (b, g, r, a bytes). Colours are blurred weighted
    // by their opacity, so transparent pixels do not darken the edges.
    static byte[] Soften(byte[] px, int n, int radius)
    {
        float[] cur = new float[n * n * 4], tmp = new float[n * n * 4];
        for (int i = 0; i < n * n; i++)
        {
            float a = px[i * 4 + 3] / 255f;
            for (int k = 0; k < 3; k++) cur[i * 4 + k] = px[i * 4 + k] / 255f * a;
            cur[i * 4 + 3] = a;
        }
        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) for (int k = 0; k < 4; k++)
            {
                float s = 0; for (int d = -radius; d <= radius; d++) { int xx = x + d; if (xx >= 0 && xx < n) s += cur[(y * n + xx) * 4 + k]; }
                tmp[(y * n + x) * 4 + k] = s / (2 * radius + 1);
            }
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) for (int k = 0; k < 4; k++)
            {
                float s = 0; for (int d = -radius; d <= radius; d++) { int yy = y + d; if (yy >= 0 && yy < n) s += tmp[(yy * n + x) * 4 + k]; }
                cur[(y * n + x) * 4 + k] = s / (2 * radius + 1);
            }
        }
        byte[] res = new byte[n * n * 4];
        for (int i = 0; i < n * n; i++)
        {
            float a = cur[i * 4 + 3];
            for (int k = 0; k < 3; k++) res[i * 4 + k] = (byte)(a > 0.001f ? RockBake.C01(cur[i * 4 + k] / a) * 255f : 0f);
            res[i * 4 + 3] = (byte)(RockBake.C01(a) * 255f);
        }
        return res;
    }

    public static string Bake(string outFile, int size)
    {
        Build();
        double minX = 1e30, maxX = -1e30, minY = 1e30, maxY = -1e30;
        foreach (var t in tris) for (int c = 0; c < 3; c++)
        {
            minX = Math.Min(minX, t.p[c][0]); maxX = Math.Max(maxX, t.p[c][0]);
            minY = Math.Min(minY, t.p[c][1]); maxY = Math.Max(maxY, t.p[c][1]);
        }
        int SS = 2, W = size * SS;
        double scale = 0.95 * W / Math.Max(maxX - minX, maxY - minY);
        double ox = (minX + maxX) / 2 - (W / 2.0) / scale, oy = (minY + maxY) / 2 - (W / 2.0) / scale;

        float[] zb = new float[W * W]; for (int i = 0; i < zb.Length; i++) zb[i] = float.NegativeInfinity;
        float[] nb = new float[W * W * 3], ob = new float[W * W * 3], mb = new float[W * W];
        RockBake.MatBuf = mb;
        foreach (var t in tris) RockBake.Raster(t, W, scale, ox, oy, zb, nb, ob, null, 0);
        RockBake.MatBuf = null;

        float[] rgb = new float[W * W * 3];
        float[] L = { -0.42f, 0.52f, 0.74f };
        int aoR1 = Math.Max(2, W / 260), aoR2 = Math.Max(4, W / 110); float aoRange = W * 0.012f;
        for (int y = 0; y < W; y++) for (int x = 0; x < W; x++)
        {
            int i = y * W + x; if (float.IsNegativeInfinity(zb[i])) continue;
            int mat = (int)mb[i];
            // The lights are almost dead: a bright dot would look like something to fly to.
            if (mat == 3) { rgb[i * 3] = 0.30f; rgb[i * 3 + 1] = 0.20f; rgb[i * 3 + 2] = 0.14f; continue; }

            float nx = nb[i * 3], ny = nb[i * 3 + 1], nz = nb[i * 3 + 2];
            float nl = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz); if (nl < 1e-6f) nl = 1f; nx /= nl; ny /= nl; nz /= nl;
            float ndv = Math.Max(0f, nz), ndl = Math.Max(0f, nx * L[0] + ny * L[1] + nz * L[2]);

            float occ = 0f; int cnt = 0;
            for (int s = 0; s < 16; s++)
            {
                double ang = s * Math.PI * 2 / 8 + (s >= 8 ? 0.39 : 0); int rad = s < 8 ? aoR1 : aoR2;
                int sx = x + (int)(Math.Cos(ang) * rad), sy = y + (int)(Math.Sin(ang) * rad);
                if (sx < 0 || sy < 0 || sx >= W || sy >= W) continue;
                float sz = zb[sy * W + sx]; cnt++;
                if (float.IsNegativeInfinity(sz)) continue;
                occ += RockBake.C01((sz - zb[i]) / aoRange);
            }
            float ao = 1f - 1.3f * (cnt > 0 ? occ / cnt : 0f); if (ao < 0.25f) ao = 0.25f;

            double qx = ob[i * 3], qy = ob[i * 3 + 1], qz = ob[i * 3 + 2];
            float wear = 0.82f + 0.36f * RockBake.Noise3(qx * 0.9, qy * 0.9, qz * 0.9);
            if (RockBake.Noise3(qx * 0.22 + 7.3, qy * 0.22, qz * 0.22) > 0.60f) wear *= 0.55f;   // scorched patches

            float r, g, b;
            if (mat == 0)
            {
                // Hull plating: seams along the axis and around it.
                double turn = Math.Atan2(qy, qx) / (Math.PI * 2) * 40, along = qz / 1.4;
                bool seam = turn - Math.Floor(turn) < 0.07 || along - Math.Floor(along) < 0.06;
                float k = wear * (seam ? 0.6f : 1f);
                r = 0.60f * k; g = 0.64f * k; b = 0.72f * k;
            }
            else if (mat == 1) { r = 0.06f * wear; g = 0.10f * wear; b = 0.22f * wear; }
            else { r = 0.30f * wear; g = 0.32f * wear; b = 0.38f * wear; }

            // Far away and in shadow: almost no direct light, only a thin cold edge
            // on the side turned to the light, and everything sunk into blue haze.
            float diff = (0.05f + 0.10f * ndv + 0.26f * ndl) * ao;
            float lit = RockBake.C01(nx * -0.62f + ny * 0.70f + 0.15f);
            float rim = (float)Math.Pow(1f - ndv, 2.6) * 0.50f * lit;
            float cr = r * diff + rim * 0.35f, cg = g * diff + rim * 0.55f, cb = b * diff + rim * 0.95f;
            const float haze = 0.30f;
            rgb[i * 3] = RockBake.C01(cr * (1f - haze) + 0.020f * haze);
            rgb[i * 3 + 1] = RockBake.C01(cg * (1f - haze) + 0.035f * haze);
            rgb[i * 3 + 2] = RockBake.C01(cb * (1f - haze) + 0.090f * haze);
        }

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
        px = Soften(px, size, 2);   // slightly out of focus, unlike the crisp things in the game

        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data); bmp.Save(outFile, ImageFormat.Png); bmp.Dispose();
        return "station wreck: " + tris.Count + " triangles";
    }
}
