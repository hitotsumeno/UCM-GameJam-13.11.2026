using System.Collections.Generic;
using UnityEngine;

// The game's own lettering: a 5x7 pixel font and small pixel icons, drawn dot by dot
// right here. Nothing is imported, so it looks the same in the editor and in a build.
// Only capital letters, digits and a few signs exist; other characters become spaces.
public static class PixelFont
{
    const int W = 5, H = 7;

    static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        { 'A', ".###.#...##...#######...##...##...#" }, { 'B', "####.#...##...#####.#...##...#####." },
        { 'C', ".###.#...##....#....#....#...#.###." }, { 'D', "####.#...##...##...##...##...#####." },
        { 'E', "######....#....####.#....#....#####" }, { 'F', "######....#....####.#....#....#...." },
        { 'G', ".###.#...##....#.####...##...#.###." }, { 'H', "#...##...##...#######...##...##...#" },
        { 'I', ".###...#....#....#....#....#...###." }, { 'J', "..###...#....#....#....#.#..#..##.." },
        { 'K', "#...##..#.#.#..##...#.#..#..#.#...#" }, { 'L', "#....#....#....#....#....#....#####" },
        { 'M', "#...###.###.#.##.#.##...##...##...#" }, { 'N', "#...###..##.#.##..###...##...##...#" },
        { 'O', ".###.#...##...##...##...##...#.###." }, { 'P', "####.#...##...#####.#....#....#...." },
        { 'Q', ".###.#...##...##...##.#.##..#..##.#" }, { 'R', "####.#...##...#####.#.#..#..#.#...#" },
        { 'S', ".#####....#.....###.....#....#####." }, { 'T', "#####..#....#....#....#....#....#.." },
        { 'U', "#...##...##...##...##...##...#.###." }, { 'V', "#...##...##...##...##...#.#.#...#.." },
        { 'W', "#...##...##...##.#.##.#.###.###...#" }, { 'X', "#...##...#.#.#...#...#.#.#...##...#" },
        { 'Y', "#...##...#.#.#...#....#....#....#.." }, { 'Z', "#####....#...#...#...#...#....#####" },
        { '0', ".###.#...##..###.#.###..##...#.###." }, { '1', "..#...##....#....#....#....#...###." },
        { '2', ".###.#...#....#...#...#...#...#####" }, { '3', "####.....#....#.###.....#....#####." },
        { '4', "...#...##..#.#.#..#.#####...#....#." }, { '5', "######....####.....#....##...#.###." },
        { '6', ".###.#....#....####.#...##...#.###." }, { '7', "#####....#...#...#...#....#....#..." },
        { '8', ".###.#...##...#.###.#...##...#.###." }, { '9', ".###.#...##...#.####....#....#.###." },
        { '-', "...............#####..............." }, { '.', ".........................##...##..." },
        { ':', ".....##...##.......##...##........." }, { '/', "....#....#...#...#...#...#....#...." },
        { '+', ".......#....#..#####..#....#......." }, { '!', "..#....#....#....#....#.........#.." },
    };

    // Icons, row by row from the top. '#' is a lit pixel.
    // "O2" with a small, lowered 2: the air.
    public static readonly string[] O2 =
    {
        ".###.....", "#...#....", "#...#....", "#...#....", "#...#.###", "#...#...#", ".###..###", "......#..", "......###",
    };
    // A crack in the hull: one per leak.
    public static readonly string[] Crack = { "...#.", "..#..", ".##..", "...#.", "..##.", ".#...", "#...." };
    // Danger markers: a meteor with its trail, and a spiked mine.
    public static readonly string[] MeteorSign = { "......#", "....##.", "..###..", ".####..", "#####..", "#####..", ".###..." };
    public static readonly string[] MineSign = { "...#...", ".#.#.#.", "..###..", "#######", "..###..", ".#.#.#.", "...#..." };
    public static readonly string[] Arrow = { "...#...", "..###..", ".#####.", "#######", "..###..", "..###..", "..###.." };

    static Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public static void ResetStatics()
    {
        cache = new Dictionary<string, Texture2D>();
    }

    // One line of text as a crisp white-on-transparent picture, 7 pixels tall.
    public static Texture2D Text(string text)
    {
        text = (text ?? "").ToUpperInvariant();
        if (text.Length == 0) text = " ";
        Texture2D tex;
        if (cache.TryGetValue("t:" + text, out tex) && tex != null) return tex;

        int width = text.Length * (W + 1) - 1;
        var pixels = new Color32[width * H];
        for (int i = 0; i < text.Length; i++)
        {
            string g;
            if (!Glyphs.TryGetValue(text[i], out g)) continue; // unknown character: a space
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (g[y * W + x] == '#')
                    pixels[(H - 1 - y) * width + i * (W + 1) + x] = new Color32(255, 255, 255, 255);
        }
        tex = Make(width, H, pixels);
        cache["t:" + text] = tex;
        return tex;
    }

    public static Texture2D Icon(string name, string[] rows)
    {
        Texture2D tex;
        if (cache.TryGetValue("i:" + name, out tex) && tex != null) return tex;

        int h = rows.Length, w = rows[0].Length;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            if (rows[y][x] == '#') pixels[(h - 1 - y) * w + x] = new Color32(255, 255, 255, 255);
        tex = Make(w, h, pixels);
        cache["i:" + name] = tex;
        return tex;
    }

    static Texture2D Make(int w, int h, Color32[] pixels)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;   // hard pixels, no blur
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave; // never saved into a scene (the editor preview uses it too)
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
