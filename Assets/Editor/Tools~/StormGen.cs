using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Pictures of the storm at the level edge (see StormEdge.cs in the game):
//   storm_body.png  turbulent cloud wall: grey brightness + alpha density, tinted in the game.
//                   24 x 32 units at 24 px per unit. Across (x): 4 units of soft front inside the
//                   level, the edge at x = 4, then 20 units of storm. Along (y): repeats seamlessly.
//   storm_glow.png  only the bright folds of the same storm, laid over the body and moved separately.
//   storm_bolt1..3  lightning bolts (white core, soft glow), 2.56 x 5.12 units.
// Run: storm.ps1 -Out <Assets\_Resources\Art>
public static class StormGen
{
    const int PPU = 24;
    const float Across = 24f, Along = 32f, Edge = 4f;

    // Value noise that repeats every `period` cells along y.
    static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
    static int Wrap(int v, int p) { int m = v % p; return m < 0 ? m + p : m; }
    static float Noise(float x, float y, int period)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        int y0 = Wrap(yi, period), y1 = Wrap(yi + 1, period);
        float a = Hash(xi, y0), b = Hash(xi + 1, y0), c = Hash(xi, y1), d = Hash(xi + 1, y1);
        return (a + (b - a) * fx) + ((c + (d - c) * fx) - (a + (b - a) * fx)) * fy;
    }
    // Octaves double the frequency exactly, so every octave repeats too.
    static float Fbm(float x, float y, int period, int oct)
    {
        float s = 0, amp = 0.5f, f = 1;
        for (int i = 0; i < oct; i++) { s += amp * Noise(x * f, y * f, period * (int)f); f *= 2; amp *= 0.5f; }
        return s;
    }
    static float Smooth(float a, float b, float t) { t = Math.Max(0, Math.Min(1, (t - a) / (b - a))); return t * t * (3 - 2 * t); }
    static float C01(float t) { return t < 0 ? 0 : (t > 1 ? 1 : t); }

    public static void Make(string outDir)
    {
        int w = (int)(Across * PPU), h = (int)(Along * PPU);
        var body = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var glow = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var bp = new int[w * h]; var gp = new int[w * h];
        // Scales chosen so that Along * scale is a whole number: the noise repeats along y.
        const float sq = 5f / 32f, sn = 7f / 32f;
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
            {
                float x = (px + 0.5f) / PPU, y = (py + 0.5f) / PPU;
                float q = Fbm(x * sq + 3.1f, y * sq, 5, 4), r = Fbm(x * sq, y * sq + 7.7f, 5, 4);
                float n = Fbm(x * sn + q * 2.2f, y * sn + r * 2.2f, 7, 5);
                float d = x - Edge + (q - 0.5f) * 5f;                       // ragged front
                float wall = Smooth(-3.5f, 4f, d);
                float dens = wall * (0.45f + 0.9f * n);
                float ridge = (float)Math.Pow(C01(1 - Math.Abs(n - 0.55f) * 4f), 3) * wall;
                // Body: dark core turning to the storm colour where it is dense.
                float grey = C01(0.28f + 0.72f * dens);
                float a = C01(dens * 0.9f);
                int gb = (int)(grey * 255);
                bp[py * w + px] = ((int)(a * 255) << 24) | (gb << 16) | (gb << 8) | gb;
                // Glow: bright folds.
                float ga = C01(ridge * 1.1f + dens * dens * 0.18f);
                gp[py * w + px] = ((int)(ga * 255) << 24) | 0xFFFFFF;
            }
        Save(body, bp, Path.Combine(outDir, "storm_body.png"));
        Save(glow, gp, Path.Combine(outDir, "storm_glow.png"));

        var rnd = new Random(5);
        for (int k = 1; k <= 3; k++) Bolt(rnd, Path.Combine(outDir, "storm_bolt" + k + ".png"));
    }

    static void Save(Bitmap b, int[] px, string path)
    {
        var d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(px, 0, d.Scan0, px.Length); b.UnlockBits(d);
        b.Save(path, ImageFormat.Png);
    }

    // A jagged bolt from the top to the bottom of a 256 x 512 picture, with a side branch.
    static void Bolt(Random rnd, string path)
    {
        var b = new Bitmap(256, 512, PixelFormat.Format32bppArgb);
        var g = Graphics.FromImage(b);
        g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
        var main = Zigzag(rnd, 128, 20, 128, 492, 14, 60);
        int at = 5 + rnd.Next(5);
        var branch = Zigzag(rnd, main[at].X, main[at].Y, main[at].X + (rnd.Next(2) == 0 ? -90 : 90), main[at].Y + 150, 6, 35);
        foreach (var p in new[] { main, branch })
        {
            float k = p == main ? 1f : 0.6f;
            g.DrawLines(new Pen(Color.FromArgb(30, 255, 255, 255), 26 * k) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round }, p);
            g.DrawLines(new Pen(Color.FromArgb(70, 255, 255, 255), 12 * k) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round }, p);
            g.DrawLines(new Pen(Color.FromArgb(150, 255, 255, 255), 5 * k) { LineJoin = LineJoin.Round }, p);
            g.DrawLines(new Pen(Color.FromArgb(255, 255, 255, 255), 2.2f * k) { LineJoin = LineJoin.Round }, p);
        }
        g.Dispose();
        b.Save(path, ImageFormat.Png);
    }

    static PointF[] Zigzag(Random rnd, float x0, float y0, float x1, float y1, int steps, float jitter)
    {
        var pts = new PointF[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float j = (i == 0 || i == steps) ? 0 : (float)(rnd.NextDouble() - 0.5) * 2 * jitter;
            pts[i] = new PointF(x0 + (x1 - x0) * t + j, y0 + (y1 - y0) * t);
        }
        return pts;
    }
}
