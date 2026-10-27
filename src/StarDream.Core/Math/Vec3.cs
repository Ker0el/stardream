using System;

namespace StarDream.Core.Math;

/// <summary>Three component vector. Right handed, Y up.</summary>
public readonly struct Vec3 : IEquatable<Vec3>
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vec3 Zero => default;
    public static Vec3 One => new(1f, 1f, 1f);
    public static Vec3 UnitX => new(1f, 0f, 0f);
    public static Vec3 UnitY => new(0f, 1f, 0f);
    public static Vec3 UnitZ => new(0f, 0f, 1f);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vec3 operator *(float s, Vec3 a) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vec3 operator /(Vec3 a, float s) => new(a.X / s, a.Y / s, a.Z / s);
    public static Vec3 operator -(Vec3 a) => new(-a.X, -a.Y, -a.Z);

    public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public static Vec3 Lerp(Vec3 a, Vec3 b, float t) => new(
        a.X + (b.X - a.X) * t,
        a.Y + (b.Y - a.Y) * t,
        a.Z + (b.Z - a.Z) * t);

    public float LengthSquared => X * X + Y * Y + Z * Z;
    public float Length => MathF.Sqrt(LengthSquared);

    public Vec3 Normalized()
    {
        var length = Length;
        return length > 1e-8f ? new Vec3(X / length, Y / length, Z / length) : Zero;
    }

    public bool IsFinite => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);

    public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override bool Equals(object? obj) => obj is Vec3 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public static bool operator ==(Vec3 a, Vec3 b) => a.Equals(b);
    public static bool operator !=(Vec3 a, Vec3 b) => !a.Equals(b);
    public override string ToString() => $"({X}, {Y}, {Z})";
}
