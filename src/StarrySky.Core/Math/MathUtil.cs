namespace StarrySky.Core.Math;

public static class MathUtil
{
    public const float DegToRad = 0.017453292519943295f;
    public const float RadToDeg = 57.29577951308232f;

    public static float Clamp(float value, float min, float max)
        => value < min ? min : value > max ? max : value;

    public static Vec3 DegToRadians(Vec3 degrees)
        => new(degrees.X * DegToRad, degrees.Y * DegToRad, degrees.Z * DegToRad);
}
