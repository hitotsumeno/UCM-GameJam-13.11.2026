using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// One level read from Assets/Resources/Levels/levelN.txt.
//
// Lines starting with # are settings (# name=ADRIFT). The rest is the map:
// every character is a cell of 2x2 units, the first map line is the top of the level.
//   P player   S ship   . empty   o oxygen tank
//   a small rock (1 cell)   A medium rock (2x2 cells)   B large rock (3x3 cells)
//   1..9 place where a hint is shown (# hint1=TEXT)
// Unknown characters are ignored, so files can already contain symbols for later steps.
public class LevelData
{
    public int index;                 // 1 for level1.txt
    public string name = "";
    public string radio = "";         // one line of story shown before the level
    public Color tint = Color.white;  // colour of the nebula
    public float oxygen = Tuning.OxygenMax; // oxygen at the start
    public bool meteors;
    public float meteorMin = Tuning.MeteorIntervalMin;
    public float meteorMax = Tuning.MeteorIntervalMax;
    public float movers;              // share of rocks that fly noticeably (0..1)
    public float star3 = 50f, star2 = 25f; // % of oxygen left for 3 and 2 stars
    public Dictionary<char, string> hints = new Dictionary<char, string>();
    public List<string> rows = new List<string>();
    public int cols;

    public Rect Bounds
    {
        get
        {
            float w = cols * Tuning.CellSize, h = rows.Count * Tuning.CellSize;
            return new Rect(-w * 0.5f, -h * 0.5f, w, h);
        }
    }

    public Vector2 CellCenter(int col, int row)
    {
        Rect b = Bounds;
        return new Vector2(b.xMin + (col + 0.5f) * Tuning.CellSize, b.yMax - (row + 0.5f) * Tuning.CellSize);
    }

    // How many level files exist: level1, level2, ... without gaps.
    public static int Count()
    {
        int n = 0;
        while (Resources.Load<TextAsset>("Levels/level" + (n + 1)) != null) n++;
        return n;
    }

    // Returns null if the file does not exist.
    public static LevelData Load(int index)
    {
        var asset = Resources.Load<TextAsset>("Levels/level" + index);
        if (asset == null) return null;
        var level = Parse(asset.text);
        level.index = index;
        return level;
    }

    public static LevelData Parse(string text)
    {
        var d = new LevelData();
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimStart('﻿').TrimEnd('\r', ' ', '\t');
            if (line.Length == 0) continue;
            if (line[0] == '#') { d.ParseSetting(line.Substring(1)); continue; }
            d.rows.Add(line);
            if (line.Length > d.cols) d.cols = line.Length;
        }
        // Short lines are filled with empty cells so every row has the same length.
        for (int i = 0; i < d.rows.Count; i++)
            if (d.rows[i].Length < d.cols) d.rows[i] = d.rows[i].PadRight(d.cols, '.');
        return d;
    }

    void ParseSetting(string s)
    {
        int eq = s.IndexOf('=');
        if (eq < 0) return;
        string key = s.Substring(0, eq).Trim().ToLowerInvariant();
        string val = s.Substring(eq + 1).Trim();

        switch (key)
        {
            case "name": name = val; break;
            case "radio": radio = val; break;
            case "oxygen": oxygen = Number(val, oxygen); break;
            case "movers": movers = Mathf.Clamp01(Number(val, 0f)); break;
            case "tint":
            {
                string[] p = val.Split(',');
                if (p.Length >= 3) tint = new Color(Number(p[0], 1f), Number(p[1], 1f), Number(p[2], 1f), 1f);
                break;
            }
            case "stars":
            {
                string[] p = val.Split(',');
                if (p.Length >= 2) { star3 = Number(p[0], star3); star2 = Number(p[1], star2); }
                break;
            }
            case "meteors":
            {
                string v = val.ToLowerInvariant();
                if (v == "off" || v == "no" || v == "0") { meteors = false; break; }
                meteors = true;
                string[] p = val.Split('-');
                if (p.Length >= 2)
                {
                    meteorMin = Number(p[0], meteorMin);
                    meteorMax = Mathf.Max(meteorMin, Number(p[1], meteorMax));
                }
                break;
            }
            default:
                if (key.Length == 5 && key.StartsWith("hint")) hints[key[4]] = val;
                break;
        }
    }

    // Numbers are always written with a dot (0.5), whatever the language of the computer.
    static float Number(string s, float fallback)
    {
        float v;
        return float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : fallback;
    }
}
