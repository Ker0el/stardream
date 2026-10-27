using System.Collections.Generic;
using System.Linq;
using StarrySky.Core.Math;
using StarrySky.Core.Meshes;

namespace StarrySky.Core.Editing;

/// <summary>Adjacency queries used by the editing operations.</summary>
public static class MeshQueries
{
    public static List<int> FacesOfEdge(Mesh mesh, Edge edge)
    {
        var result = new List<int>();
        for (var f = 0; f < mesh.Faces.Count; f++)
        {
            if (ContainsEdge(mesh.Faces[f], edge))
            {
                result.Add(f);
            }
        }

        return result;
    }

    public static bool ContainsEdge(int[] face, Edge edge)
    {
        for (var i = 0; i < face.Length; i++)
        {
            var a = face[i];
            var b = face[(i + 1) % face.Length];
            if ((a == edge.A && b == edge.B) || (a == edge.B && b == edge.A))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Edges used by exactly one of the given faces: the open border of a patch.</summary>
    public static List<Edge> BoundaryEdges(Mesh mesh, IReadOnlySet<int> faces)
    {
        var counts = new Dictionary<Edge, int>();

        foreach (var faceIndex in faces)
        {
            if (faceIndex < 0 || faceIndex >= mesh.Faces.Count)
            {
                continue;
            }

            var face = mesh.Faces[faceIndex];
            for (var i = 0; i < face.Length; i++)
            {
                var edge = new Edge(face[i], face[(i + 1) % face.Length]);
                counts[edge] = counts.TryGetValue(edge, out var n) ? n + 1 : 1;
            }
        }

        return counts.Where(kv => kv.Value == 1).Select(kv => kv.Key).ToList();
    }

    /// <summary>Splits a face set into groups connected through shared edges.</summary>
    public static List<HashSet<int>> ConnectedComponents(Mesh mesh, IReadOnlySet<int> faces)
    {
        var remaining = new HashSet<int>(faces);
        var result = new List<HashSet<int>>();

        while (remaining.Count > 0)
        {
            var seed = remaining.First();
            remaining.Remove(seed);

            var component = new HashSet<int> { seed };
            var queue = new Queue<int>();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var face = mesh.Faces[current];

                for (var i = 0; i < face.Length; i++)
                {
                    var edge = new Edge(face[i], face[(i + 1) % face.Length]);
                    foreach (var neighbour in FacesOfEdge(mesh, edge))
                    {
                        if (remaining.Remove(neighbour))
                        {
                            component.Add(neighbour);
                            queue.Enqueue(neighbour);
                        }
                    }
                }
            }

            result.Add(component);
        }

        return result;
    }

    public static Vec3 Centroid(Mesh mesh, EditSelection selection)
    {
        var vertices = selection.ResolveVertices(mesh);
        if (vertices.Count == 0)
        {
            return Vec3.Zero;
        }

        var sum = Vec3.Zero;
        foreach (var index in vertices)
        {
            sum += mesh.Positions[index];
        }

        return sum / vertices.Count;
    }

    public static Vec3 AverageNormal(Mesh mesh, IReadOnlySet<int> faces)
    {
        var sum = Vec3.Zero;
        foreach (var faceIndex in faces)
        {
            if (faceIndex >= 0 && faceIndex < mesh.FaceNormals.Length)
            {
                sum += mesh.FaceNormals[faceIndex];
            }
        }

        return sum.Normalized();
    }
}
