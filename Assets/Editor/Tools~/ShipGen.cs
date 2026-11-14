using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Draws placeholder pictures for the player's rocket, its side gun and the station.
// Everything is drawn nose/barrel to the RIGHT, as the game expects.
public static class ShipGen
{
    static PointF P(float x, float y) { return new PointF(x, y); }
    static Pen Outline(float w) { var p = new Pen(Color.FromArgb(24, 24, 34), w); p.LineJoin = LineJoin.Round; p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; return p; }
    static Graphics Begin(Bitmap bmp) { var g = Graphics.FromImage(bmp); g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent); return g; }

    // Toy rocket: light hull, two round windows, blue tail section, orange fins. No flame: it is out of fuel.
    public static void Rocket(string path)
    {
        var bmp = new Bitmap(512, 256, PixelFormat.Format32bppArgb);
        using (var g = Begin(bmp))
        {
            float c = 128f; var o = Outline(7f);

            // Fins (behind the hull).
            for (int s = -1; s <= 1; s += 2)
            {
                PointF[] fin = { P(92, c + s * 50), P(210, c + s * 56), P(74, c + s * 116), P(38, c + s * 104) };
                using (var b = new LinearGradientBrush(new RectangleF(30, 0, 190, 256), Color.FromArgb(250, 205, 70), Color.FromArgb(240, 120, 25), 0f))
                    g.FillPolygon(b, fin);
                g.DrawPolygon(o, fin);
            }

            // Nozzle.
            PointF[] noz = { P(72, c - 36), P(72, c + 36), P(16, c + 46), P(16, c - 46) };
            using (var b = new LinearGradientBrush(new RectangleF(0, c - 48, 80, 96), Color.FromArgb(70, 140, 225), Color.FromArgb(20, 50, 130), 90f))
                g.FillPolygon(b, noz);
            using (var stripe = new Pen(Color.FromArgb(16, 36, 96), 4f))
                for (int x = 30; x <= 58; x += 14) g.DrawLine(stripe, x, c - 43 + (x - 16) * 0.16f, x, c + 43 - (x - 16) * 0.16f);
            g.DrawPolygon(o, noz);

            // Hull.
            var hull = new GraphicsPath();
            hull.AddBezier(P(64, c - 60), P(200, c - 80), P(390, c - 62), P(490, c));
            hull.AddBezier(P(490, c), P(390, c + 62), P(200, c + 80), P(64, c + 60));
            hull.CloseFigure();
            using (var b = new LinearGradientBrush(new RectangleF(0, c - 82, 512, 164), Color.FromArgb(252, 252, 255), Color.FromArgb(140, 146, 162), 90f))
                g.FillPath(b, hull);

            // Blue tail section, clipped to the hull.
            g.SetClip(hull);
            using (var b = new LinearGradientBrush(new RectangleF(0, c - 82, 512, 164), Color.FromArgb(70, 130, 235), Color.FromArgb(18, 46, 128), 90f))
                g.FillRectangle(b, 60, 0, 142, 256);
            using (var stripe = new Pen(Color.FromArgb(14, 30, 90), 5f))
                for (int x = 96; x <= 172; x += 38) g.DrawLine(stripe, x, 0, x, 256);
            using (var shine = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                g.FillRectangle(shine, 60, c - 52, 440, 14);
            using (var edge = Outline(6f)) g.DrawLine(edge, 202, 0, 202, 256);
            g.ResetClip();
            g.DrawPath(o, hull);

            // Fin along the middle of the tail section.
            PointF[] mid = { P(70, c), P(128, c - 13), P(216, c), P(128, c + 13) };
            using (var b = new LinearGradientBrush(new RectangleF(60, c - 14, 160, 28), Color.FromArgb(250, 205, 70), Color.FromArgb(236, 110, 22), 90f))
                g.FillPolygon(b, mid);
            using (var thin = Outline(5f)) g.DrawPolygon(thin, mid);

            // Windows.
            Window(g, 348, c, 35);
            Window(g, 262, c, 24);
        }
        bmp.Save(path, ImageFormat.Png);
    }

    static void Window(Graphics g, float x, float y, float r)
    {
        using (var rim = new SolidBrush(Color.FromArgb(245, 150, 30))) g.FillEllipse(rim, x - r - 9, y - r - 9, 2 * r + 18, 2 * r + 18);
        using (var o = Outline(5f)) g.DrawEllipse(o, x - r - 9, y - r - 9, 2 * r + 18, 2 * r + 18);
        using (var b = new LinearGradientBrush(new RectangleF(x - r, y - r, 2 * r, 2 * r), Color.FromArgb(120, 200, 255), Color.FromArgb(20, 90, 190), 60f))
            g.FillEllipse(b, x - r, y - r, 2 * r, 2 * r);
        using (var o = Outline(4f)) g.DrawEllipse(o, x - r, y - r, 2 * r, 2 * r);
        using (var w = new SolidBrush(Color.FromArgb(230, 255, 255, 255))) g.FillEllipse(w, x - r * 0.55f, y - r * 0.6f, r * 0.42f, r * 0.42f);
    }

    // Side gun, barrel to the right. The mount (the point it swings around) is 40 px from the left edge.
    public static void Gun(string path)
    {
        var bmp = new Bitmap(256, 96, PixelFormat.Format32bppArgb);
        using (var g = Begin(bmp))
        {
            float c = 48f; var o = Outline(6f);
            var barrel = new RectangleF(40, c - 14, 190, 28);
            using (var b = new LinearGradientBrush(new RectangleF(0, c - 16, 256, 32), Color.FromArgb(215, 220, 230), Color.FromArgb(96, 102, 118), 90f))
                g.FillRectangle(b, barrel);
            using (var s = new SolidBrush(Color.FromArgb(245, 150, 30))) g.FillRectangle(s, 112, c - 14, 18, 28);
            g.DrawRectangle(o, barrel.X, barrel.Y, barrel.Width, barrel.Height);

            var muzzle = new RectangleF(206, c - 20, 34, 40);
            using (var b = new LinearGradientBrush(new RectangleF(0, c - 22, 256, 44), Color.FromArgb(120, 126, 140), Color.FromArgb(50, 54, 66), 90f))
                g.FillRectangle(b, muzzle);
            g.DrawRectangle(o, muzzle.X, muzzle.Y, muzzle.Width, muzzle.Height);

            using (var b = new SolidBrush(Color.FromArgb(70, 76, 92))) g.FillEllipse(b, 40 - 30, c - 30, 60, 60);
            g.DrawEllipse(o, 40 - 30, c - 30, 60, 60);
            using (var b = new SolidBrush(Color.FromArgb(245, 150, 30))) g.FillEllipse(b, 40 - 11, c - 11, 22, 22);
        }
        bmp.Save(path, ImageFormat.Png);
    }

    // Round station with two solar panels and a green docking light.
    public static void Station(string path)
    {
        var bmp = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
        using (var g = Begin(bmp))
        {
            float c = 256f; var o = Outline(8f);

            // Truss and solar panels.
            using (var b = new SolidBrush(Color.FromArgb(150, 156, 170))) g.FillRectangle(b, 30, c - 12, 452, 24);
            g.DrawRectangle(o, 30, c - 12, 452, 24);
            for (int s = -1; s <= 1; s += 2)
            {
                var panel = new RectangleF(s < 0 ? 22 : 366, c - 92, 124, 184);
                using (var b = new LinearGradientBrush(panel, Color.FromArgb(70, 140, 235), Color.FromArgb(20, 54, 140), 60f)) g.FillRectangle(b, panel);
                using (var grid = new Pen(Color.FromArgb(14, 30, 90), 3f))
                {
                    for (int i = 1; i < 3; i++) g.DrawLine(grid, panel.X + panel.Width * i / 3f, panel.Y, panel.X + panel.Width * i / 3f, panel.Bottom);
                    for (int i = 1; i < 4; i++) g.DrawLine(grid, panel.X, panel.Y + panel.Height * i / 4f, panel.Right, panel.Y + panel.Height * i / 4f);
                }
                g.DrawRectangle(o, panel.X, panel.Y, panel.Width, panel.Height);
            }

            // Outer ring.
            using (var dark = Outline(34f)) g.DrawEllipse(dark, c - 150, c - 150, 300, 300);
            using (var ring = new Pen(Color.FromArgb(205, 210, 222), 20f)) g.DrawEllipse(ring, c - 150, c - 150, 300, 300);

            // Hub.
            using (var b = new LinearGradientBrush(new RectangleF(c - 110, c - 110, 220, 220), Color.FromArgb(250, 250, 255), Color.FromArgb(130, 136, 152), 70f))
                g.FillEllipse(b, c - 110, c - 110, 220, 220);
            g.DrawEllipse(o, c - 110, c - 110, 220, 220);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4 + 0.39;
                using (var w = new SolidBrush(Color.FromArgb(40, 130, 215)))
                    g.FillEllipse(w, c + (float)Math.Cos(a) * 80 - 9, c + (float)Math.Sin(a) * 80 - 9, 18, 18);
            }

            // Docking port with a green light.
            using (var b = new SolidBrush(Color.FromArgb(40, 44, 56))) g.FillEllipse(b, c - 44, c - 44, 88, 88);
            using (var light = new Pen(Color.FromArgb(80, 235, 125), 9f)) g.DrawEllipse(light, c - 36, c - 36, 72, 72);
            g.DrawEllipse(o, c - 44, c - 44, 88, 88);
        }
        bmp.Save(path, ImageFormat.Png);
    }

    // Same numbers as in the game's Tuning (units).
    public static float StrutRootX = 0f, StrutRootY = 0.26f, StrutThickness = 0.09f;

    // The ship as the game shows it: hull turned to `heading`, guns swung to aim at `aim` (degrees, counter-clockwise).
    // Mount position and gun length are in units, like in the game's Tuning.
    public static void DrawShip(Graphics g, Bitmap hull, Bitmap gun, float cx, float cy, float pxPerUnit,
        float heading, float aim, float shipLength, float gunLength, float mountX, float mountY)
    {
        float a = aim - heading; while (a > 180) a -= 360; while (a < -180) a += 360;
        float left = a >= 0 ? a : -a, right = -left;
        var state = g.Save();
        g.TranslateTransform(cx, cy); g.RotateTransform(-heading);
        // Struts from the hull to the guns hanging by the engine (drawn under the hull).
        using (var strut = new Pen(Color.FromArgb(92, 97, 115), StrutThickness * pxPerUnit))
            for (int s = -1; s <= 1; s += 2)
                g.DrawLine(strut, StrutRootX * pxPerUnit, -s * StrutRootY * pxPerUnit, mountX * pxPerUnit, -s * mountY * pxPerUnit);
        float w = shipLength * pxPerUnit, h = w * hull.Height / hull.Width;
        g.DrawImage(hull, -w / 2, -h / 2, w, h);
        for (int s = -1; s <= 1; s += 2)
        {
            var st = g.Save();
            g.TranslateTransform(mountX * pxPerUnit, -s * mountY * pxPerUnit);
            g.RotateTransform(-(s > 0 ? left : right));
            float len = gunLength * pxPerUnit, gh = len * gun.Height / gun.Width;
            g.DrawImage(gun, -len * 40f / 256f, -gh / 2, len, gh);
            g.Restore(st);
        }
        g.Restore(state);
    }

    // Three big poses on a dark background, and the same ship at game scale next to a rock and the station.
    public static void Preview(string artDir, string background, string outPath)
    {
        var hull = new Bitmap(artDir + "\\ship.png"); var gun = new Bitmap(artDir + "\\ship_gun.png");
        var bg = new Bitmap(background);
        var bmp = new Bitmap(1120, 630 + 330, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.FromArgb(10, 12, 28));
            // Close-ups: heading up; aiming back (thrust), aiming to the upper-left, aiming to the right.
            float[] aims = { 270f, 140f, 10f };
            for (int i = 0; i < 3; i++)
                DrawShip(g, hull, gun, 190 + i * 370, 150, 105f, 90f, aims[i], 2.4f, 0.95f, 0f, 0.68f);
            // Game scale (camera size 10: 31.5 px per unit).
            g.DrawImage(bg, 0, 330, 1120, 630);
            float ppu = 31.5f;
            using (var st = new Bitmap(artDir + "\\station.png")) g.DrawImage(st, 820, 330 + 60, 6 * ppu, 6 * ppu);
            using (var r1 = new Bitmap(artDir + "\\asteroid_m1.png")) g.DrawImage(r1, 420, 330 + 250, 4 * ppu, 4 * ppu);
            using (var r2 = new Bitmap(artDir + "\\asteroid_s2.png")) g.DrawImage(r2, 640, 330 + 420, 2 * ppu, 2 * ppu);
            DrawShip(g, hull, gun, 250, 330 + 380, ppu, 60f, 240f, 2.4f, 0.95f, 0f, 0.68f);
            DrawShip(g, hull, gun, 560, 330 + 150, ppu, 0f, 200f, 2.4f, 0.95f, 0f, 0.68f);
        }
        bmp.Save(outPath, ImageFormat.Png);
    }
}
