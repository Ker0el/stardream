using System;
using System.Collections.Generic;
using System.Numerics;
using StarDream.Core.Math;

namespace StarDream.Core.Meshes;

/// <summary>
/// Polygon mesh: positions plus a list of faces, each holding vertex indices in
/// counter-clockwise order. Edges, triangulation and normals are derived data,
/// rebuilt with <see cref="RebuildDerived"/> after any topology change.
/// </summary>
public sealed class Mesh
{
    public List<Vec3> Positions { get; } = new();
    public List<int[]> Faces { get; } = new();

    public List<Edge> Edges { get; } = new();
    public List<int[]> Triangles { get; } = new();

    public Vec3[] FaceNormals { get; private set; } = Array.Empty<Vec3>();

    /// <summary>One normal per face corner, in face order. Filled by the normal smoother.</summary>
    public Vec3[] CornerNormals { get; set; } = Array.Empty<Vec3>();

    public int CornerCount
    {
        get
        {
            var total = 0;
            foreach (var face in Faces)
            {
                total += face.Length;
            }

            return total;
        }
    }

    /// <summary>Index of the first corner of <paramref name="faceIndex"/> in <see cref="CornerNormals"/>.</summary>
    public int CornerOffset(int faceIndex)
    {
        if (faceIndex < 0 || faceIndex > Faces.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(faceIndex));
        }

        var offset = 0;
        for (var i = 0; i < faceIndex; i++)
        {
            offset += Faces[i].Length;
        }

        return offset;
    }

    public void RebuildDerived()
    {
        Validate();

        BuildEdges();
        BuildTriangles();
        BuildFaceNormals();

        if (CornerNormals.Length != CornerCount)
        {
            CornerNormals = new Vec3[CornerCount];
        }
    }

    private void Validate()
    {
        for (var f = 0; f < Faces.Count; f++)
        {
            var face = Faces[f];
            if (face.Length < 3)
            {
                throw new InvalidOperationException($"Face {f} has fewer than three vertices.");
            }

            foreach (var index in face)
            {
                if (index < 0 || index >= Positions.Count)
                {
                    throw new InvalidOperationException($"Face {f} references vertex {index}, out of range.");
                }
            }
        }
    }

    private void BuildEdges()
    {
        Edges.Clear();
        var seen = new HashSet<Edge>();

        foreach (var face in Faces)
        {
            for (var i = 0; i < face.Length; i++)
            {
                var a = face[i];
                var b = face[(i + 1) % face.Length];
                if (a == b)
                {
                    continue;
                }

                var edge = new Edge(a, b);
                if (seen.Add(edge))
                {
                    Edges.Add(edge);
                }
            }
        }

        Edges.Sort((x, y) => x.A != y.A ? x.A.CompareTo(y.A) : x.B.CompareTo(y.B));
    }

    private void BuildTriangles()
    {
        Triangles.Clear();

        foreach (var face in Faces)
        {
            for (var i = 1; i < face.Length - 1; i++)
            {
                Triangles.Add(new[] { face[0], face[i], face[i + 1] });
            }
        }
    }

    private void BuildFaceNormals()
    {
        FaceNormals = new Vec3[Faces.Count];

        for (var f = 0; f < Faces.Count; f++)
        {
            var face = Faces[f];

            // Newell's method: stable for non-planar and concave polygons.
            double nx = 0, ny = 0, nz = 0;
            for (var i = 0; i < face.Length; i++)
            {
                var current = Positions[face[i]];
                var next = Positions[face[(i + 1) % face.Length]];

                nx += (current.Y - next.Y) * (current.Z + next.Z);
                ny += (current.Z - next.Z) * (current.X + next.X);
                nz += (current.X - next.X) * (current.Y + next.Y);
            }

            var normal = new Vec3((float)nx, (float)ny, (float)nz).Normalized();
            FaceNormals[f] = normal.LengthSquared > 0.5f ? normal : Vec3.UnitY;
        }
    }

    public Mesh Clone()
    {
        var clone = new Mesh();
        clone.Positions.AddRange(Positions);

        foreach (var face in Faces)
        {
            clone.Faces.Add((int[])face.Clone());
        }

        clone.RebuildDerived();
        clone.CornerNormals = (Vec3[])CornerNormals.Clone();
        return clone;
    }

    public void Clear()
    {
        Positions.Clear();
        Faces.Clear();
        Edges.Clear();
        Triangles.Clear();
        FaceNormals = Array.Empty<Vec3>();
        CornerNormals = Array.Empty<Vec3>();
    }
}
