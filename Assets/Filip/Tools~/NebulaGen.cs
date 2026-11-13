using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Periodic (seamlessly tiling) Perlin gradient noise.
public class PNoise
{
    int[] perm = new int[512];
    public PNoise(int seed)
    {
        var rng = new Random(seed);
        int[] p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;
        for (int i = 255; i > 0; i--) { int j = rng.Next(i + 1); int t = p[i]; p[i] = p[j]; p[j] = t; }
        for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
    }
    static float Fade(float t) { return t * t * t * (t * (t * 6f - 15f) + 10f); }
    static float Grad(int h, float x, float y)
    {
        switch (h & 7)
        {
            case 0: return x + y; case 1: return x - y; case 2: return -x + y; case 3: return -x - y;
            case 4: return x; case 5: return -x; case 6: return y; default: return -y;
        }
    }
    static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    public float Noise(float x, float y, int period)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        float fx = x - xi, fy = y - yi;
        int x0 = ((xi % period) + period) % period, y0 = ((yi % period) + period) % period;
        int x1 = (x0 + 1) % period, y1 = (y0 + 1) % period;
        float u = Fade(fx), v = Fade(fy);
        float n00 = Grad(perm[perm[x0] + y0], fx, fy);
        float n10 = Grad(perm[perm[x1] + y0], fx - 1f, fy);
        float n01 = Grad(perm[perm[x0] + y1], fx, fy - 1f);
        float n11 = Grad(perm[perm[x1] + y1], fx - 1f, fy - 1f);
        return Lerp(Lerp(n00, n10, u), Lerp(n01, n11, u), v);
    }
    public float Fbm(float u, float v, int basePeriod, int octaves, float gain)
    {
        float sum = 0f, amp = 1f, total = 0f; int period = basePeriod;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * Noise(u * period, v * period, period);
            total += amp; amp *= gain; period *= 2;
        }
        return sum / total;
    }
}

public static class NebulaGen
{
    static float Clamp01(float t) { return t < 0f ? 0f : (t > 1f ? 1f : t); }
    static float Smooth(float lo, float hi, float x) { float t = Clamp01((x - lo) / (hi - lo)); return t * t * (3f - 2f * t); }
    static byte B(float v) { int i = (int)Math.Round(v * 255f); return (byte)(i < 0 ? 0 : (i > 255 ? 255 : i)); }

    // Colour ramp that wraps around: t in 0..1.
    static void Palette(float[][] stops, float t, out float r, out float g, out float b)
    {
        t = t - (float)Math.Floor(t);
        float f = t * stops.Length; int i = (int)f; float k = f - i; k = k * k * (3f - 2f * k);
        float[] a = stops[i % stops.Length], c = stops[(i + 1) % stops.Length];
        r = a[0] + (c[0] - a[0]) * k; g = a[1] + (c[1] - a[1]) * k; b = a[2] + (c[2] - a[2]) * k;
    }

    static readonly float[][] FarStops = {
        new float[] { 0.30f, 0.10f, 0.55f },   // violet
        new float[] { 0.08f, 0.20f, 0.60f },   // blue
        new float[] { 0.05f, 0.38f, 0.50f },   // teal
        new float[] { 0.12f, 0.16f, 0.62f },   // indigo
        new float[] { 0.50f, 0.10f, 0.45f },   // magenta
    };
    static readonly float[][] NearStops = {
        new float[] { 0.62f, 0.22f, 0.95f },   // bright violet
        new float[] { 0.95f, 0.30f, 0.70f },   // pink
        new float[] { 0.25f, 0.50f, 1.00f },   // blue
        new float[] { 0.20f, 0.85f, 0.90f },   // cyan
        new float[] { 0.40f, 0.35f, 1.00f },   // indigo
    };

    // Far layer: opaque, dim, big soft clouds over a near-black sky.
    public static byte[] Far(int R, int seed)
    {
        var W1 = new PNoise(seed + 1); var W2 = new PNoise(seed + 2);
        var D = new PNoise(seed + 3); var H = new PNoise(seed + 4); var K = new PNoise(seed + 5);
        byte[] px = new byte[R * R * 4];
        for (int y = 0; y < R; y++) for (int x = 0; x < R; x++)
        {
            float u = (x + 0.5f) / R, v = (y + 0.5f) / R;
            float wx = W1.Fbm(u, v, 2, 3, 0.5f), wy = W2.Fbm(u, v, 2, 3, 0.5f);
            float d = D.Fbm(u + 0.22f * wx, v + 0.22f * wy, 2, 6, 0.52f);
            float c = Smooth(-0.30f, 0.22f, d);
            float dust = Smooth(-0.05f, 0.35f, K.Fbm(u + 0.1f * wy, v + 0.1f * wx, 3, 5, 0.55f));
            c *= 1f - 0.55f * dust;
            float h = H.Fbm(u + 0.1f * wx, v, 2, 3, 0.5f) * 1.6f + 0.5f;
            float pr, pg, pb; Palette(FarStops, h, out pr, out pg, out pb);
            float glow = c * c * (0.35f + 0.65f * c);
            float r = 0.010f + pr * glow * 0.85f;
            float g = 0.013f + pg * glow * 0.85f;
            float b = 0.032f + pb * glow * 0.85f + 0.03f * c;
            int i = ((R - 1 - y) * R + x) * 4;
            px[i] = B(b); px[i + 1] = B(g); px[i + 2] = B(r); px[i + 3] = 255;
        }
        return px;
    }

    // Near layer: transparent, brighter wisps with glowing cores.
    public static byte[] Near(int R, int seed)
    {
        var W1 = new PNoise(seed + 11); var W2 = new PNoise(seed + 12);
        var D = new PNoise(seed + 13); var H = new PNoise(seed + 14); var Rg = new PNoise(seed + 15);
        byte[] px = new byte[R * R * 4];
        for (int y = 0; y < R; y++) for (int x = 0; x < R; x++)
        {
            float u = (x + 0.5f) / R, v = (y + 0.5f) / R;
            float wx = W1.Fbm(u, v, 3, 4, 0.5f), wy = W2.Fbm(u, v, 3, 4, 0.5f);
            float uu = u + 0.30f * wx, vv = v + 0.30f * wy;
            float d = D.Fbm(uu, vv, 3, 6, 0.55f);
            float ridge = 1f - Math.Abs(Rg.Fbm(uu, vv, 5, 5, 0.55f)) * 3.2f;
            ridge = Clamp01(ridge); ridge = ridge * ridge;
            float c = Smooth(-0.10f, 0.36f, d + 0.16f * (ridge - 0.35f));
            float h = H.Fbm(u, v + 0.1f * wy, 2, 3, 0.5f) * 1.8f + 0.2f;
            float pr, pg, pb; Palette(NearStops, h, out pr, out pg, out pb);
            float core = c * c * c;
            float lum = 0.50f + 0.35f * c;
            float r = pr * lum + 0.55f * core;
            float g = pg * lum + 0.50f * core;
            float b = pb * lum + 0.60f * core;
            float a = c * (0.30f + 0.38f * c);
            int i = ((R - 1 - y) * R + x) * 4;
            px[i] = B(b); px[i + 1] = B(g); px[i + 2] = B(r); px[i + 3] = B(a);
        }
        return px;
    }

    public static void Save(byte[] px, int R, string path)
    {
        var bmp = new Bitmap(R, R, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, R, R), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data);
        bmp.Save(path, ImageFormat.Png);
        bmp.Dispose();
    }

    // Preview of what the screen sees: a 28x16 unit window, near layer over far layer, with parallax offsets.
    public static void Screen(byte[] far, int RF, float farUnits, byte[] near, int RN, float nearUnits,
        float camX, float camY, float farK, float nearK, string path)
    {
        int W = 1120, Hh = 640; float ppu = 40f;
        byte[] px = new byte[W * Hh * 4];
        for (int y = 0; y < Hh; y++) for (int x = 0; x < W; x++)
        {
            float wx = (x - W / 2) / ppu, wy = (Hh / 2 - y) / ppu;   // units from screen centre
            float[] f = Sample(far, RF, (wx + camX * farK) / farUnits, (wy + camY * farK) / farUnits);
            float[] n = Sample(near, RN, (wx + camX * nearK) / nearUnits, (wy + camY * nearK) / nearUnits);
            float a = n[3];
            int i = (y * W + x) * 4;
            px[i] = B(n[0] * a + f[0] * (1 - a)); px[i + 1] = B(n[1] * a + f[1] * (1 - a));
            px[i + 2] = B(n[2] * a + f[2] * (1 - a)); px[i + 3] = 255;
        }
        var bmp = new Bitmap(W, Hh, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, W, Hh), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data);
        bmp.Save(path, ImageFormat.Png);
        bmp.Dispose();
    }

    // Bilinear, wrapping. Returns b,g,r,a in 0..1. v grows upward.
    static float[] Sample(byte[] px, int R, float u, float v)
    {
        float x = (u - (float)Math.Floor(u)) * R - 0.5f, y = (1f - (v - (float)Math.Floor(v))) * R - 0.5f;
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y); float fx = x - x0, fy = y - y0;
        float[] o = new float[4];
        for (int k = 0; k < 4; k++)
        {
            float p00 = px[(Mod(y0, R) * R + Mod(x0, R)) * 4 + k], p10 = px[(Mod(y0, R) * R + Mod(x0 + 1, R)) * 4 + k];
            float p01 = px[(Mod(y0 + 1, R) * R + Mod(x0, R)) * 4 + k], p11 = px[(Mod(y0 + 1, R) * R + Mod(x0 + 1, R)) * 4 + k];
            o[k] = ((p00 + (p10 - p00) * fx) * (1 - fy) + (p01 + (p11 - p01) * fx) * fy) / 255f;
        }
        return o;
    }
    static int Mod(int a, int n) { return ((a % n) + n) % n; }
}
