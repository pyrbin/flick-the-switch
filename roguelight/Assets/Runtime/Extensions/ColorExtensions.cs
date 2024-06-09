
using UnityEngine;

public static class ColorExtensions
{
    public static Color IncreaseLuminosity(this Color color, float amount)
    {
        float h, s, l;
        ColorExtensions.RGBToHSL(color, out h, out s, out l);

        l += amount;
        l = Mathfs.Clamp01(l); // Ensure the lightness stays between 0 and 1

        return ColorExtensions.HSLToRGB(h, s, l);
    }

    public static void RGBToHSL(this Color color, out float h, out float s, out float l)
    {
        float r = color.r;
        float g = color.g;
        float b = color.b;

        float max = Mathfs.Max(r, g, b);
        float min = Mathfs.Min(r, g, b);
        float delta = max - min;

        l = (max + min) / 2f;

        if (delta == 0f)
        {
            h = 0f;
            s = 0f;
        }
        else
        {
            if (l < 0.5f)
                s = delta / (max + min);
            else
                s = delta / (2f - max - min);

            if (r == max)
                h = (g - b) / delta;
            else if (g == max)
                h = 2f + (b - r) / delta;
            else
                h = 4f + (r - g) / delta;

            h *= 60f;
            if (h < 0f)
                h += 360f;
        }
    }

    public static Color HSLToRGB(float h, float s, float l)
    {
        float r, g, b;

        if (s == 0f)
        {
            r = g = b = l; // achromatic
        }
        else
        {
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            r = HueToRGB(p, q, h + 1f / 3f);
            g = HueToRGB(p, q, h);
            b = HueToRGB(p, q, h - 1f / 3f);
        }

        return new Color(r, g, b);
    }

    private static float HueToRGB(float p, float q, float t)
    {
        if (t < 0f) t += 1f;
        if (t > 1f) t -= 1f;
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
        return p;
    }
}
