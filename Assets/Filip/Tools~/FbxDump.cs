using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;

public class FNode
{
    public string Name;
    public List<object> Props = new List<object>();
    public List<FNode> Children = new List<FNode>();
    public FNode Find(string name) { foreach (var c in Children) if (c.Name == name) return c; return null; }
}

public static class FbxDump
{
    static FNode ReadNode(BinaryReader r, long fileLen)
    {
        uint end = r.ReadUInt32(), numProps = r.ReadUInt32(), propLen = r.ReadUInt32();
        byte nameLen = r.ReadByte();
        if (end == 0) return null;
        var n = new FNode();
        n.Name = Encoding.ASCII.GetString(r.ReadBytes(nameLen));
        for (int i = 0; i < numProps; i++) n.Props.Add(ReadProp(r));
        while (r.BaseStream.Position < end)
        {
            var c = ReadNode(r, fileLen);
            if (c == null) break;
            n.Children.Add(c);
        }
        r.BaseStream.Position = end;
        return n;
    }

    static object ReadProp(BinaryReader r)
    {
        char t = (char)r.ReadByte();
        switch (t)
        {
            case 'Y': return r.ReadInt16();
            case 'C': return r.ReadByte();
            case 'I': return r.ReadInt32();
            case 'F': return r.ReadSingle();
            case 'D': return r.ReadDouble();
            case 'L': return r.ReadInt64();
            case 'S': { int len = r.ReadInt32(); return Encoding.UTF8.GetString(r.ReadBytes(len)); }
            case 'R': { int len = r.ReadInt32(); return r.ReadBytes(len); }
            case 'f': case 'd': case 'l': case 'i': case 'b':
            {
                int count = r.ReadInt32(); int enc = r.ReadInt32(); int clen = r.ReadInt32();
                byte[] raw = r.ReadBytes(clen);
                if (enc == 1)
                {
                    var ms = new MemoryStream(raw, 2, raw.Length - 2);
                    var ds = new DeflateStream(ms, CompressionMode.Decompress);
                    var outp = new MemoryStream(); ds.CopyTo(outp); raw = outp.ToArray();
                }
                var br = new BinaryReader(new MemoryStream(raw));
                if (t == 'd') { var a = new double[count]; for (int i = 0; i < count; i++) a[i] = br.ReadDouble(); return a; }
                if (t == 'f') { var a = new double[count]; for (int i = 0; i < count; i++) a[i] = br.ReadSingle(); return a; }
                if (t == 'i') { var a = new int[count]; for (int i = 0; i < count; i++) a[i] = br.ReadInt32(); return a; }
                if (t == 'l') { var a = new long[count]; for (int i = 0; i < count; i++) a[i] = br.ReadInt64(); return a; }
                return raw;
            }
        }
        throw new Exception("Unknown property type " + t);
    }

    static string Clean(string s) { int i = s.IndexOf('\0'); return i >= 0 ? s.Substring(0, i) : s; }

    // Draws every mesh of the file as a white silhouette with a soft glow: <outDir>/<model name>.png
    public static string BakeSprites(string path, string outDir, int size)
    {
        var sb = new StringBuilder();
        var bytes = File.ReadAllBytes(path);
        var r = new BinaryReader(new MemoryStream(bytes));
        r.BaseStream.Position = 27;
        var root = new FNode();
        while (r.BaseStream.Position < bytes.Length - 13)
        {
            var n = ReadNode(r, bytes.Length);
            if (n == null) break;
            root.Children.Add(n);
        }
        var modelNames = new Dictionary<long, string>();
        var geos = new Dictionary<long, FNode>();
        foreach (var o in root.Find("Objects").Children)
        {
            if (o.Name == "Model") modelNames[(long)o.Props[0]] = Clean((string)o.Props[1]);
            if (o.Name == "Geometry") geos[(long)o.Props[0]] = o;
        }
        foreach (var c in root.Find("Connections").Children)
        {
            long a = (long)c.Props[1], b = (long)c.Props[2];
            if (!geos.ContainsKey(a) || !modelNames.ContainsKey(b)) continue;
            var geo = geos[a];
            double[] v = (double[])geo.Find("Vertices").Props[0];
            int[] idx = (int[])geo.Find("PolygonVertexIndex").Props[0];
            double[] mn = { 1e30, 1e30, 1e30 }, mx = { -1e30, -1e30, -1e30 };
            for (int i = 0; i < v.Length; i++) { int k = i % 3; if (v[i] < mn[k]) mn[k] = v[i]; if (v[i] > mx[k]) mx[k] = v[i]; }
            int thin = 0; for (int k = 1; k < 3; k++) if (mx[k] - mn[k] < mx[thin] - mn[thin]) thin = k;
            int ax = thin == 0 ? 1 : 0, ay = thin == 2 ? 1 : 2;
            double span = Math.Max(mx[ax] - mn[ax], mx[ay] - mn[ay]);
            double cx = (mn[ax] + mx[ax]) / 2, cy = (mn[ay] + mx[ay]) / 2;

            // Sharp shape, drawn 4x bigger without smoothing and then scaled down (no seams between polygons).
            // The shape takes only 42% of the picture: the rest is room for the glow.
            int big = size * 4;
            double sc = big * 0.42 / span;
            var hi = new Bitmap(big, big, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(hi))
            {
                g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.None;
                var poly = new List<PointF>();
                foreach (int k in idx)
                {
                    int vi = k < 0 ? ~k : k;
                    poly.Add(new PointF((float)(big / 2 + (v[vi * 3 + ax] - cx) * sc), (float)(big / 2 - (v[vi * 3 + ay] - cy) * sc)));
                    if (k < 0)
                    {
                        if (poly.Count >= 3) { g.FillPolygon(Brushes.White, poly.ToArray()); g.DrawPolygon(Pens.White, poly.ToArray()); }
                        poly.Clear();
                    }
                }
            }
            var core = Resize(hi, size);
            float[] al = new float[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) al[y * size + x] = core.GetPixel(x, y).A / 255f;

            // Glow: the shape blurred at three sizes (tight, medium, wide) plus a round aura, fading out to the edges.
            float[] g1 = Blur(al, size, size / 64), g2 = Blur(al, size, size / 20), g3 = Blur(al, size, size / 8);
            var coreBmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            var glowBmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
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
                glowBmp.SetPixel(x, y, Color.FromArgb((int)(C01(glow) * 255f), 255, 255, 255));
                coreBmp.SetPixel(x, y, Color.FromArgb((int)(al[i] * 255f), 255, 255, 255));
            }
            string nm = modelNames[b];                       // star1, star2, ...
            string digits = nm.TrimStart("abcdefghijklmnopqrstuvwxyz_".ToCharArray());
            string prefix = nm.Substring(0, nm.Length - digits.Length);
            coreBmp.Save(Path.Combine(outDir, nm + ".png"), ImageFormat.Png);
            glowBmp.Save(Path.Combine(outDir, prefix + "_glow" + digits + ".png"), ImageFormat.Png);
            sb.AppendLine(nm + ".png + " + prefix + "_glow" + digits + ".png");
        }
        return sb.ToString();
    }

    static float C01(float t) { return t < 0f ? 0f : (t > 1f ? 1f : t); }

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

    // Test picture: stars with glow drawn over a background image, at the sizes the game uses.
    public static void Preview(string artDir, string background, string outPath, float ppu)
    {
        var bg = new Bitmap(background);
        var bmp = new Bitmap(bg.Width, bg.Height, PixelFormat.Format32bppArgb);
        var rng = new Random(5);
        float[][] palette = {
            new float[] { 0.65f, 0.82f, 1.00f }, new float[] { 0.55f, 0.95f, 1.00f }, new float[] { 0.78f, 0.65f, 1.00f },
            new float[] { 1.00f, 0.68f, 0.88f }, new float[] { 1.00f, 0.82f, 0.50f }, new float[] { 1f, 1f, 1f } };
        using (var g = Graphics.FromImage(bmp))
        {
            g.DrawImage(bg, 0, 0, bg.Width, bg.Height);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // dust
            for (int i = 0; i < 110; i++)
            {
                float s = (0.03f + (float)rng.NextDouble() * 0.05f) * ppu;
                int al = 60 + rng.Next(110);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(Color.FromArgb(al, 215, 230, 255)), (float)rng.NextDouble() * bg.Width, (float)rng.NextDouble() * bg.Height, s, s);
            }
            // far (small) and near (big) stars
            for (int i = 0; i < 36; i++)
            {
                bool near = i < 9;
                float shape = near ? 0.38f + (float)rng.NextDouble() * 0.42f : 0.13f + (float)rng.NextDouble() * 0.17f;
                float canvas = shape / 0.42f * ppu;
                float bright = near ? 0.75f + (float)rng.NextDouble() * 0.25f : 0.45f + (float)rng.NextDouble() * 0.45f;
                int k = 1 + rng.Next(5); float[] c = palette[rng.Next(palette.Length)];
                float x = (float)rng.NextDouble() * bg.Width, y = (float)rng.NextDouble() * bg.Height;
                var rect = new RectangleF(x - canvas / 2, y - canvas / 2, canvas, canvas);
                Draw(g, Path.Combine(artDir, "star_glow" + k + ".png"), rect, c[0], c[1], c[2], 0.9f * bright);
                Draw(g, Path.Combine(artDir, "star" + k + ".png"), rect, 0.75f + 0.25f * c[0], 0.75f + 0.25f * c[1], 0.75f + 0.25f * c[2], bright);
            }
        }
        bmp.Save(outPath, ImageFormat.Png);
    }

    static void Draw(Graphics g, string file, RectangleF rect, float r, float gr, float b, float alpha)
    {
        using (var img = new Bitmap(file))
        {
            var cm = new ColorMatrix(new float[][] {
                new float[] { r, 0, 0, 0, 0 }, new float[] { 0, gr, 0, 0, 0 }, new float[] { 0, 0, b, 0, 0 },
                new float[] { 0, 0, 0, alpha, 0 }, new float[] { 0, 0, 0, 0, 1 } });
            var ia = new ImageAttributes(); ia.SetColorMatrix(cm);
            g.DrawImage(img, new PointF[] { new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top), new PointF(rect.Left, rect.Bottom) },
                new RectangleF(0, 0, img.Width, img.Height), GraphicsUnit.Pixel, ia);
        }
    }

    static Bitmap Resize(Bitmap src, int size)
    {
        var dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(dst))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(src, new Rectangle(0, 0, size, size), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, WrapAttr());
        }
        return dst;
    }

    static ImageAttributes WrapAttr() { var a = new ImageAttributes(); a.SetWrapMode(WrapMode.TileFlipXY); return a; }

    public static string Run(string path, string pngOut)
    {
        var sb = new StringBuilder();
        var bytes = File.ReadAllBytes(path);
        var r = new BinaryReader(new MemoryStream(bytes));
        r.BaseStream.Position = 23; uint version = r.ReadUInt32();
        sb.AppendLine("FBX version " + version);
        var root = new FNode(); root.Name = "root";
        while (r.BaseStream.Position < bytes.Length - 13)
        {
            var n = ReadNode(r, bytes.Length);
            if (n == null) break;
            root.Children.Add(n);
        }

        var gs = root.Find("GlobalSettings");
        if (gs != null) foreach (var p in gs.Find("Properties70").Children)
        {
            string nm = (string)p.Props[0];
            if (nm.Contains("Axis") || nm.Contains("UnitScale")) sb.AppendLine("  " + nm + " = " + p.Props[p.Props.Count - 1]);
        }

        var objects = root.Find("Objects");
        var names = new Dictionary<long, string>();
        var geos = new List<FNode>();
        foreach (var o in objects.Children)
        {
            long id = (long)o.Props[0]; string nm = Clean((string)o.Props[1]);
            names[id] = o.Name + ":" + nm;
            if (o.Name == "Geometry") geos.Add(o);
            if (o.Name == "Model")
            {
                sb.AppendLine("Model '" + nm + "' type=" + o.Props[2]);
                foreach (var p in o.Find("Properties70").Children)
                {
                    string pn = (string)p.Props[0];
                    if (pn.StartsWith("Lcl"))
                        sb.AppendLine("   " + pn + " = " + p.Props[4] + ", " + p.Props[5] + ", " + p.Props[6]);
                }
            }
            if (o.Name == "Material")
            {
                sb.AppendLine("Material '" + nm + "'");
                foreach (var p in o.Find("Properties70").Children)
                {
                    string pn = (string)p.Props[0];
                    if (pn.Contains("Color") && p.Props.Count >= 7)
                        sb.AppendLine("   " + pn + " = " + p.Props[4] + ", " + p.Props[5] + ", " + p.Props[6]);
                    else if (pn.Contains("Factor") || pn.Contains("Opacity"))
                        sb.AppendLine("   " + pn + " = " + p.Props[p.Props.Count - 1]);
                }
            }
            if (o.Name == "Texture" || o.Name == "Video") sb.AppendLine(o.Name + " '" + nm + "'");
        }
        var con = root.Find("Connections");
        foreach (var c in con.Children)
        {
            long a = (long)c.Props[1], b = (long)c.Props[2];
            string an = names.ContainsKey(a) ? names[a] : a.ToString(), bn = names.ContainsKey(b) ? names[b] : (b == 0 ? "scene" : b.ToString());
            sb.AppendLine("  link " + an + " -> " + bn);
        }

        int cell = 256;
        var bmp = new Bitmap(cell * Math.Max(1, geos.Count), cell, PixelFormat.Format32bppArgb);
        var g = Graphics.FromImage(bmp); g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.FromArgb(20, 20, 40));
        int gi = 0;
        foreach (var geo in geos)
        {
            string nm = Clean((string)geo.Props[1]);
            double[] v = (double[])geo.Find("Vertices").Props[0];
            int[] idx = (int[])geo.Find("PolygonVertexIndex").Props[0];
            double[] mn = { 1e30, 1e30, 1e30 }, mx = { -1e30, -1e30, -1e30 };
            for (int i = 0; i < v.Length; i++) { int a = i % 3; if (v[i] < mn[a]) mn[a] = v[i]; if (v[i] > mx[a]) mx[a] = v[i]; }
            int polys = 0, maxSides = 0, sides = 0;
            foreach (int k in idx) { sides++; if (k < 0) { polys++; if (sides > maxSides) maxSides = sides; sides = 0; } }
            sb.AppendLine(string.Format("Geometry '{0}': verts={1} polys={2} maxSides={3} size x={4:0.###} y={5:0.###} z={6:0.###}  min=({7:0.###},{8:0.###},{9:0.###})",
                nm, v.Length / 3, polys, maxSides, mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2], mn[0], mn[1], mn[2]));
            var lec = geo.Find("LayerElementColor");
            sb.AppendLine("   vertex colours: " + (lec != null ? "yes" : "no") + ", UV: " + (geo.Find("LayerElementUV") != null ? "yes" : "no"));

            // Project on the two widest axes.
            int thin = 0; for (int a = 1; a < 3; a++) if (mx[a] - mn[a] < mx[thin] - mn[thin]) thin = a;
            int ax = thin == 0 ? 1 : 0, ay = thin == 2 ? 1 : 2;
            double span = Math.Max(mx[ax] - mn[ax], mx[ay] - mn[ay]); if (span <= 0) span = 1;
            double sc = (cell - 30) / span;
            double cx = (mn[ax] + mx[ax]) / 2, cy = (mn[ay] + mx[ay]) / 2;
            var poly = new List<PointF>();
            var brush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
            var pen = new Pen(Color.FromArgb(200, 255, 200, 80), 1f);
            foreach (int k in idx)
            {
                int vi = k < 0 ? ~k : k;
                poly.Add(new PointF((float)(gi * cell + cell / 2 + (v[vi * 3 + ax] - cx) * sc), (float)(cell / 2 - (v[vi * 3 + ay] - cy) * sc)));
                if (k < 0) { if (poly.Count >= 3) { g.FillPolygon(brush, poly.ToArray()); g.DrawPolygon(pen, poly.ToArray()); } poly.Clear(); }
            }
            sb.AppendLine("   drawn on axes " + "xyz"[ax] + "/" + "xyz"[ay] + " (thin axis " + "xyz"[thin] + ")");
            gi++;
        }
        g.Dispose(); bmp.Save(pngOut, ImageFormat.Png);
        return sb.ToString();
    }
}
