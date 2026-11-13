using UnityEngine;

// Generates the placeholder space background in code: dark sky with soft purple and
// blue clouds. The picture repeats seamlessly, so it can scroll forever.
public static class Nebula
{
    const int Resolution = 512;
    const int Seed = 20261113;

    static Sprite cached;

    public static void ResetStatics()
    {
        cached = null;
    }

    // Square sprite, Tuning.BackgroundSize units on a side.
    public static Sprite GetSprite()
    {
        if (cached != null) return cached;

        var rng = new System.Random(Seed);
        var purple = new Field(rng);
        var blue = new Field(rng);
        var grain = new System.Random(Seed + 1);

        var baseColor = new Vector3(0.012f, 0.016f, 0.04f);
        var purpleColor = new Vector3(0.17f, 0.06f, 0.30f);
        var blueColor = new Vector3(0.02f, 0.13f, 0.27f);

        var pixels = new Color32[Resolution * Resolution];
        for (int y = 0; y < Resolution; y++)
        for (int x = 0; x < Resolution; x++)
        {
            float u = (x + 0.5f) / Resolution, v = (y + 0.5f) / Resolution;
            float p = Cloud(purple.Sample(u, v));
            float b = Cloud(blue.Sample(u, v));
            Vector3 c = baseColor + purpleColor * p + blueColor * b;
            // A little grain hides colour banding in the dark gradients.
            float g = ((float)grain.NextDouble() - 0.5f) / 255f;
            pixels[y * Resolution + x] = new Color32(ToByte(c.x + g), ToByte(c.y + g), ToByte(c.z + g), 255);
        }

        var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels32(pixels);
        tex.Apply();

        cached = Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution),
            new Vector2(0.5f, 0.5f), Resolution / Tuning.BackgroundSize);
        cached.name = "nebula";
        return cached;
    }

    // Turns the random field into cloud density: nothing below 0.42, full above 0.80, soft in between.
    static float Cloud(float n)
    {
        float t = Mathf.InverseLerp(0.42f, 0.80f, n);
        return t * t * (3f - 2f * t);
    }

    static byte ToByte(float v)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
    }

    // Smooth random field that repeats exactly at the picture edges (several layers of detail).
    class Field
    {
        static readonly int[] Periods = { 3, 6, 12, 24 };
        static readonly float[] Weights = { 1f, 0.45f, 0.12f, 0.04f };
        readonly float[][] lattices;

        public Field(System.Random rng)
        {
            lattices = new float[Periods.Length][];
            for (int i = 0; i < Periods.Length; i++)
            {
                int n = Periods[i];
                lattices[i] = new float[n * n];
                for (int k = 0; k < n * n; k++) lattices[i][k] = (float)rng.NextDouble();
            }
        }

        // u, v in 0..1 across the picture. Returns 0..1.
        public float Sample(float u, float v)
        {
            float sum = 0f, total = 0f;
            for (int i = 0; i < Periods.Length; i++)
            {
                sum += Weights[i] * Layer(lattices[i], Periods[i], u, v);
                total += Weights[i];
            }
            return sum / total;
        }

        static float Layer(float[] lattice, int n, float u, float v)
        {
            float x = u * n, y = v * n;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            x0 %= n; y0 %= n;
            int x1 = (x0 + 1) % n, y1 = (y0 + 1) % n;
            float a = Mathf.Lerp(lattice[y0 * n + x0], lattice[y0 * n + x1], fx);
            float b = Mathf.Lerp(lattice[y1 * n + x0], lattice[y1 * n + x1], fx);
            return Mathf.Lerp(a, b, fy);
        }
    }
}
