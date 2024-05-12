using System.Runtime.CompilerServices;

namespace SharpMania;

public static class MathExt
{
    public static float Lerp(float a, float b, float t)
    {
        return a * (1 - t) + b * t;
    }

    public static float LerpClamp(float a, float b, float t)
    {
        return Lerp(a, b, Math.Clamp(t, 0f, 1f));
    }

    public static float Animate(float a, float b, float timeStart, float time, float duration)
    {
        return Lerp(a, b, TimeProgress(timeStart, time, duration));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float TimeProgress(float timeStart, float time, float duration)
        => Math.Clamp((time - timeStart) / duration, 0f, 1f);

    public static int Hash(int x)
    {
        // https://stackoverflow.com/questions/664014/what-integer-hash-function-are-good-that-accepts-an-integer-hash-key
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = (x >> 16) ^ x;
        return x;
    }
}
