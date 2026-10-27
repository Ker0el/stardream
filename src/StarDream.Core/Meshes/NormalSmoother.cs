using System;
using System.Collections.Generic;
using StarDream.Core.Math;

namespace StarDream.Core.Meshes;

/// <summary>
/// Fills <see cref="Mesh.CornerNormals"/> from face normals. Adjacent faces whose
/// normals differ by at most <see cref="SmoothAngleDegrees"/> share one averaged
/// normal; sharper joins stay hard-edged. A cube therefore shades flat and a sphere
/// shades smooth under the same rule.
/// </summary>
public static class NormalSmoother
{
    public const float SmoothAngleDegrees = 30f;

    private static readonly float CosThreshold = MathF.Cos(SmoothAngleDegrees * MathUtil.DegToRad);

    public static void Compute(Mesh mesh)
    {
        var faceCount = mesh.Faces.Count;
        var cornerCount = mesh.CornerCount;
        var normals = new Vec3[cornerCount];

        // Vertex -> incident faces.
        var incident = new Dictionary<int, List<int>>();
        for (var f = 0; f < faceCount; f++)
        {
            foreach (var vertex in mesh.Faces[f])
            {
                if (!incident.TryGetValue(vertex, out var list))
                {
                    list = new List<int>();
                    incident[vertex] = list;
                }

                list.Add(f);
            }
        }

        for (var f = 0; f < faceCount; f++)
        {
            var face = mesh.Faces[f];
            var offset = mesh.CornerOffset(f);

            for (var c = 0; c < face.Length; c++)
            {
                var vertex = face[c];
                var group = CollectGroup(mesh, incident[vertex], f);

                var sum = Vec3.Zero;
                foreach (var other in group)
                {
                    sum += mesh.FaceNormals[other];
                }

                var normal = sum.Normalized();
                normals[offset + c] = normal.LengthSquared > 0.5f ? normal : mesh.FaceNormals[f];
            }
        }

        mesh.CornerNormals = normals;
    }

    /// <summary>Every face reachable from <paramref name="seed"/> through joins under the smoothing angle.</summary>
    private static List<int> CollectGroup(Mesh mesh, List<int> faces, int seed)
    {
        var result = new List<int>();
        var seen = new HashSet<int>();

        var stack = new Stack<int>();
        stack.Push(seed);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current))
            {
                continue;
            }

            result.Add(current);

            foreach (var candidate in faces)
            {
                if (seen.Contains(candidate))
                {
                    continue;
                }

                if (Vec3.Dot(mesh.FaceNormals[current], mesh.FaceNormals[candidate]) >= CosThreshold)
                {
                    stack.Push(candidate);
                }
            }
        }

        return result;
    }
}
