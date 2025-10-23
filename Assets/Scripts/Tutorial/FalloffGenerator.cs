using UnityEngine;

public static class FalloffGenerator
{
    /// <summary>
    /// Generates a falloff map where 0 = water, 1 = land edge.
    /// </summary>
    public static float[,] GenerateFalloffMap(int size, float falloffScale = 1f, float edgeSharpness = 3f)
    {
        float[,] map = new float[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)size * 2 - 1;
                float ny = y / (float)size * 2 - 1;

                // Distance from center (bigger = farther from island center)
                float value = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                // Apply scale control — higher = smaller island
                value /= Mathf.Max(0.0001f, falloffScale);
                value = Mathf.Clamp01(value);

                float falloff = Evaluate(value, edgeSharpness);
                map[x, y] = falloff;
            }
        }
        return map;
    }

    static float Evaluate(float value, float edgeSharpness)
    {
        float a = edgeSharpness;
        float b = 2.2f; // Controls curve bias
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
