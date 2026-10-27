using System;
using System.Collections.Generic;
using System.Linq;
using StarrySky.Core.Math;
using StarrySky.Core.Meshes;

namespace StarrySky.Core.Editing;

public sealed class ExtrudeResult
{
    public List<int> NewFaceIndices { get; } = new();
    public HashSet<int> NewVertexIndices { get; } = new();
    public string? Error { get; set; }
    public bool Succeeded => Error is null;
}

/// <summary>Topology changing edit operations.</summary>
public static class MeshEditor
{
    public const float DefaultExtrudeDistance = 0.2f;

    public static void Delete(Mesh mesh, EditSelection selection)
    {
        if (selection.IsEmpty)
        {
            return;
        }

        var doomed = selection.ResolveFaces(mesh);
        if (doomed.Count == 0)
        {
            return;
        }

        var kept = new List<int[]>();
        for (var f = 0; f < mesh.Faces.Count; f++)
        {
            if (!doomed.Contains(f))
            {
                kept.Add(mesh.Faces[f]);
            }
        }

        mesh.Faces.Clear();
        mesh.Faces.AddRange(kept);

        RemoveOrphanVertices(mesh);
        mesh.RebuildDerived();
        NormalSmoother.Compute(mesh);
        selection.Clear();
    }

    public static ExtrudeResult Extrude(Mesh mesh, EditSelection selection, float distance)
    {
        var result = new ExtrudeResult();

        if (selection.Mode != ElementMode.Face)
        {
            result.Error = "挤出只对面选择生效";
            return result;
        }

        if (selection.IsEmpty)
        {
            result.Error = "没有选中任何面";
            return result;
        }

        var components = MeshQueries.ConnectedComponents(mesh, selection.Faces);

        foreach (var component in components)
        {
            var boundary = MeshQueries.BoundaryEdges(mesh, component);

            if (boundary.Count == 0)
            {
                result.Error = "闭合选区没有可挤出的边界";
                return result;
            }

            var normal = MeshQueries.AverageNormal(mesh, component);
            if (normal.Length < 0.1f)
            {
                result.Error = "选中面的法线互相抵消，无法确定挤出方向";
                return result;
            }

            var vertices = new HashSet<int>();
            foreach (var faceIndex in component)
            {
                foreach (var vertex in mesh.Faces[faceIndex])
                {
                    vertices.Add(vertex);
                }
            }

            var mapping = new Dictionary<int, int>(vertices.Count);
            foreach (var vertex in vertices)
            {
                var moved = mesh.Positions[vertex] + normal * distance;
                mapping[vertex] = mesh.Positions.Count;
                mesh.Positions.Add(moved);
                result.NewVertexIndices.Add(mapping[vertex]);
            }

            foreach (var faceIndex in component)
            {
                var face = mesh.Faces[faceIndex];
                for (var i = 0; i < face.Length; i++)
                {
                    face[i] = mapping[face[i]];
                }

                result.NewFaceIndices.Add(faceIndex);
            }

            foreach (var edge in boundary)
            {
                mesh.Faces.Add(new[]
                {
                    edge.A,
                    edge.B,
                    mapping[edge.B],
                    mapping[edge.A],
                });
            }
        }

        mesh.RebuildDerived();
        NormalSmoother.Compute(mesh);

        selection.Faces.Clear();
        selection.Faces.UnionWith(result.NewFaceIndices);
        return result;
    }

    private static void RemoveOrphanVertices(Mesh mesh)
    {
        var used = new bool[mesh.Positions.Count];
        foreach (var face in mesh.Faces)
        {
            foreach (var vertex in face)
            {
                if (vertex >= 0 && vertex < used.Length)
                {
                    used[vertex] = true;
                }
            }
        }

        var mapping = new int[mesh.Positions.Count];
        var positions = new List<Vec3>(mesh.Positions.Count);

        for (var i = 0; i < mesh.Positions.Count; i++)
        {
            if (used[i])
            {
                mapping[i] = positions.Count;
                positions.Add(mesh.Positions[i]);
            }
            else
            {
                mapping[i] = -1;
            }
        }

        if (positions.Count == mesh.Positions.Count)
        {
            return;
        }

        mesh.Positions.Clear();
        mesh.Positions.AddRange(positions);

        foreach (var face in mesh.Faces)
        {
            for (var i = 0; i < face.Length; i++)
            {
                face[i] = mapping[face[i]];
            }
        }
    }
}
