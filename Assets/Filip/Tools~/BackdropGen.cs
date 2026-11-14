using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// Big far-away objects that give a level its place: a distant sun, the wreck of
// a station, a ringed planet. They are dim on purpose: the background must not
// compete with the rocks.
public static class BackdropGen
{
    static float C01(float t) { return t < 0f ? 0f : (t > 1f ? 1f : t); }
    static byte B(float v) { int i = (int)Math.Round(v * 255f); return (byte)(i < 0 ? 0 : (i > 255 ? 255 : i)); }

    static void Save(byte[] px, int w, int h, string path)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data); bmp.Save(path, ImageFormat.Png); bmp.Dispose();
    }

    // A small far sun: bright point, wide soft glow, four faint rays.
    public static void Sun(string path)
    {
        int R = 512; byte[] px = new byte[R * R * 4];
        for (int y = 0; y < R; y++) for (int x = 0; x < R; x++)
        {
            float dx = (x + 0.5f - R / 2f) / (R / 2f), dy = (y + 0.5f - R / 2f) / (R / 2f);
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            float core = C01(1f - d / 0.045f);
            float glow = (float)Math.Exp(-d * d / (0.16f * 0.16f)) * 0.55f + (float)Math.Exp(-d * d / (0.42f * 0.42f)) * 0.22f;
            float ray = (float)(Math.Exp(-Math.Abs(dx) * 60) + Math.Exp(-Math.Abs(dy) * 60)) * (float)Math.Exp(-d * 3.2f) * 0.5f;
            float fade = C01((1f - d) / 0.25f); fade = fade * fade * (3 - 2 * fade);
            float a = C01(core + glow + ray) * fade;
            int i = (y * R + x) * 4;
            px[i] = B(0.80f + 0.20f * core); px[i + 1] = B(0.90f + 0.10f * core); px[i + 2] = 255; px[i + 3] = B(a);
        }
        Save(px, R, R, path);
    }

    // A ringed planet lit from the upper left: a bright crescent, the rest in shadow.
    public static void Planet(string path)
    {
        int R = 1024; byte[] px = new byte[R * R * 4];
        var bands = new PNoise(91); var swirl = new PNoise(92);
        float pr = 0.34f;                         // planet radius, share of the picture
        float lx = -0.55f, ly = 0.62f, lz = 0.56f; // light direction
        for (int y = 0; y < R; y++) for (int x = 0; x < R; x++)
        {
            float u = (x + 0.5f) / R - 0.5f, v = 0.5f - (y + 0.5f) / R;
            float d = (float)Math.Sqrt(u * u + v * v);
            float r = 0, g = 0, b = 0, a = 0;

            // Atmosphere: thin glow just outside the disc.
            float atm = (float)Math.Exp(-Math.Max(0f, d - pr) / 0.012f) * 0.55f;
            if (d > pr) { r = 0.75f; g = 0.95f; b = 0.55f; a = atm * C01((u * lx + v * ly) / d * 0.9f + 0.55f); }

            if (d <= pr)
            {
                float nx = u / pr, ny = v / pr, nz = (float)Math.Sqrt(Math.Max(0f, 1f - nx * nx - ny * ny));
                float lit = C01(nx * lx + ny * ly + nz * lz);
                float lat = ny * 2.2f + 0.12f * swirl.Fbm(nx * 0.4f + 0.5f, ny * 0.4f + 0.5f, 4, 4, 0.5f) * 6f;
                float band = bands.Fbm(0.37f, lat * 0.25f + 0.5f, 4, 5, 0.55f) * 2.2f + 0.5f; band = C01(band);
                // Dark olive <-> teal bands: the planet stays darker than the rocks in front of it.
                float cr = 0.46f - 0.26f * band, cg = 0.46f - 0.02f * band, cb = 0.20f + 0.20f * band;
                float shade = 0.05f + 0.60f * lit * lit;
                float rim = (float)Math.Pow(1f - nz, 3) * 0.45f * C01(nx * lx + ny * ly + 0.4f);
                r = cr * shade + rim * 0.7f; g = cg * shade + rim * 0.9f; b = cb * shade + rim * 0.5f; a = 1f;
                float edge = C01((pr - d) / 0.003f); a *= edge;
            }

            // Ring: a tilted ellipse of dust. Its far half is hidden behind the planet.
            float ca = (float)Math.Cos(-0.38), sa = (float)Math.Sin(-0.38);
            float ru = u * ca - v * sa, rv = (u * sa + v * ca) / 0.26f;
            float rd = (float)Math.Sqrt(ru * ru + rv * rv);
            if (rd > 0.39f && rd < 0.49f)
            {
                bool behind = rv > 0f && d <= pr;
                if (!behind)
                {
                    float t = (rd - 0.39f) / 0.10f;
                    float dens = (0.75f + 0.25f * (float)Math.Sin(t * 26f)) * C01(t / 0.12f) * C01((1f - t) / 0.12f);
                    float ra = dens * 0.26f;
                    r = r * (1 - ra) + 0.85f * ra; g = g * (1 - ra) + 0.74f * ra; b = b * (1 - ra) + 0.50f * ra;
                    a = a + ra * (1 - a);
                }
            }
            int i = (y * R + x) * 4;
            px[i] = B(b); px[i + 1] = B(g); px[i + 2] = B(r); px[i + 3] = B(a);
        }
        Save(px, R, R, path);
    }

    // The wreck of a station: dark silhouettes with a faint edge and a few dying lights.
    public static void Wreck(string path)
    {
        var bmp = new Bitmap(1024, 512, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
            var body = new SolidBrush(Color.FromArgb(235, 26, 32, 54));
            var edge = new Pen(Color.FromArgb(150, 70, 84, 124), 3f); edge.LineJoin = LineJoin.Round;

            // Broken ring: three arcs of different length.
            using (var ring = new Pen(Color.FromArgb(235, 26, 32, 54), 34f))
            using (var ringEdge = new Pen(Color.FromArgb(120, 70, 84, 124), 40f))
            {
                var rect = new Rectangle(250, 70, 380, 380);
                g.DrawArc(ringEdge, rect, 200, 120); g.DrawArc(ring, rect, 200, 120);
                g.DrawArc(ringEdge, rect, 345, 70); g.DrawArc(ring, rect, 345, 70);
                g.DrawArc(ringEdge, rect, 80, 55); g.DrawArc(ring, rect, 80, 55);
            }
            // Hub.
            g.FillEllipse(body, 370, 190, 140, 140); g.DrawEllipse(edge, 370, 190, 140, 140);
            // Truss bent in the middle.
            PointF[] truss = { new PointF(60, 300), new PointF(380, 250), new PointF(384, 272), new PointF(66, 326) };
            g.FillPolygon(body, truss); g.DrawPolygon(edge, truss);
            PointF[] truss2 = { new PointF(505, 262), new PointF(760, 190), new PointF(768, 212), new PointF(512, 284) };
            g.FillPolygon(body, truss2); g.DrawPolygon(edge, truss2);
            // Torn solar panels drifting away.
            PointF[][] panels = {
                new[] { new PointF(40, 210), new PointF(170, 190), new PointF(182, 280), new PointF(52, 300) },
                new[] { new PointF(790, 110), new PointF(900, 150), new PointF(872, 236), new PointF(766, 198) },
                new[] { new PointF(880, 320), new PointF(960, 300), new PointF(978, 360), new PointF(900, 384) },
                new[] { new PointF(210, 400), new PointF(270, 380), new PointF(292, 430), new PointF(236, 452) },
            };
            using (var panel = new SolidBrush(Color.FromArgb(225, 22, 38, 78)))
                foreach (var p in panels) { g.FillPolygon(panel, p); g.DrawPolygon(edge, p); }
            // Small debris.
            var rng = new Random(4);
            for (int i = 0; i < 26; i++)
            {
                float x = 40 + (float)rng.NextDouble() * 940, y = 40 + (float)rng.NextDouble() * 430, s = 4 + (float)rng.NextDouble() * 12;
                g.FillRectangle(body, x, y, s, s * 0.6f);
            }
            // A few lights still on.
            int[][] lights = { new[] { 440, 240, 255, 120, 60 }, new[] { 300, 268, 255, 70, 60 }, new[] { 620, 232, 120, 200, 255 }, new[] { 112, 248, 255, 150, 70 } };
            foreach (var l in lights)
            {
                using (var glow = new SolidBrush(Color.FromArgb(60, l[2], l[3], l[4]))) g.FillEllipse(glow, l[0] - 10, l[1] - 10, 20, 20);
                using (var dot = new SolidBrush(Color.FromArgb(230, l[2], l[3], l[4]))) g.FillEllipse(dot, l[0] - 3, l[1] - 3, 6, 6);
            }
        }
        bmp.Save(path, ImageFormat.Png);
    }

    // Test frame: backdrop and a few sprites drawn over a background, sizes in screen pixels.
    public static void Compose(string background, string[] files, int[] xs, int[] ys, int[] widths, float[] alphas, string outPath)
    {
        var bg = new Bitmap(background);
        var bmp = new Bitmap(bg.Width, bg.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bg, 0, 0, bg.Width, bg.Height);
            for (int i = 0; i < files.Length; i++)
                using (var img = new Bitmap(files[i]))
                {
                    int w = widths[i], h = w * img.Height / img.Width;
                    var cm = new ColorMatrix(); cm.Matrix33 = alphas[i];
                    var ia = new ImageAttributes(); ia.SetColorMatrix(cm);
                    g.DrawImage(img, new Rectangle(xs[i], ys[i], w, h), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);
                }
        }
        bmp.Save(outPath, ImageFormat.Png);
    }
}
