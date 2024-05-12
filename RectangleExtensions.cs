using Raylib_CsLo;

namespace SharpMania;

public static class RectangleExtensions
{
    public static Rectangle Lerp(this Rectangle a, Rectangle b, float t)
    {
        return new Rectangle(
            MathExt.Lerp(a.X, b.X, t),
            MathExt.Lerp(a.Y, b.Y, t),
            MathExt.Lerp(a.width, b.width, t),
            MathExt.Lerp(a.height, b.height, t));
    }
}
