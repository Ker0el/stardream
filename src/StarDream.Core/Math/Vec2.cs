using System;

namespace StarDream.Core.Math;

/// <summary>Two component vector, used for screen space positions and UV coordinates.</summary>
public readonly struct Vec2 : IEquatable<Vec2>
{
    public readonly float X;
    public readonly float Y;

    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vec2 Zero => default;
    public static Vec2 One => new(1f, 1f);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, float s) => new(a.X * s, a.Y * s);
    public static Vec2 operator *(float s, Vec2 a) => new(a.X * s, a.Y * s);
    public static Vec2 operator -(Vec2 a) => new(-a.X, -a.Y);

    public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

    public float LengthSquared => X * X + Y * Y;
    public float Length => MathF.Sqrt(LengthSquared);

    public Vec2 Normalized()
    {
        var length = Length;
        return length > 1e-8f ? new Vec2(X / length, Y / length) : Zero;
    }

    public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is Vec2 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
    public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);
    public override string ToString() => $"({X}, {Y})";
}
