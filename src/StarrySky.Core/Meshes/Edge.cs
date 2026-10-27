using System;
using System.Collections.Generic;
using StarrySky.Core.Math;

namespace StarrySky.Core.Meshes;

/// <summary>An unordered pair of vertex indices with the smaller index first.</summary>
public readonly struct Edge : IEquatable<Edge>
{
    public readonly int A;
    public readonly int B;

    public Edge(int a, int b)
    {
        if (a < b)
        {
            A = a;
            B = b;
        }
        else
        {
            A = b;
            B = a;
        }
    }

    public bool Equals(Edge other) => A == other.A && B == other.B;
    public override bool Equals(object? obj) => obj is Edge other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(A, B);
    public static bool operator ==(Edge a, Edge b) => a.Equals(b);
    public static bool operator !=(Edge a, Edge b) => !a.Equals(b);
    public override string ToString() => $"Edge({A}, {B})";
}
