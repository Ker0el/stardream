using System.Collections.Generic;
using System.Linq;
using StarrySky.Core.Meshes;

namespace StarrySky.Core.Editing;

public enum ElementMode
{
    Vertex,
    Edge,
    Face,
}

/// <summary>Selected mesh elements. Only the set for the active mode is meaningful.</summary>
public sealed class EditSelection
{
    public ElementMode Mode { get; set; } = ElementMode.Vertex;

    public HashSet<int> Vertices { get; } = new();
    public HashSet<Edge> Edges { get; } = new();
    public HashSet<int> Faces { get; } = new();

    public int Count => Mode switch
    {
        ElementMode.Vertex => Vertices.Count,
        ElementMode.Edge => Edges.Count,
        _ => Faces.Count,
    };

    public bool IsEmpty => Count == 0;

    public void Clear()
    {
        Vertices.Clear();
        Edges.Clear();
        Faces.Clear();
    }

    public void SwitchMode(ElementMode mode)
    {
        if (Mode == mode)
        {
            return;
        }

        Mode = mode;
        Clear();
    }

    /// <summary>Every vertex touched by the current selection, whichever mode is active.</summary>
    public HashSet<int> ResolveVertices(Mesh mesh)
    {
        var result = new HashSet<int>();

        switch (Mode)
        {
            case ElementMode.Vertex:
                result.UnionWith(Vertices);
                break;
            case ElementMode.Edge:
                foreach (var edge in Edges)
                {
                    result.Add(edge.A);
                    result.Add(edge.B);
                }

                break;
            case ElementMode.Face:
                foreach (var faceIndex in Faces)
                {
                    if (faceIndex >= 0 && faceIndex < mesh.Faces.Count)
                    {
                        result.UnionWith(mesh.Faces[faceIndex]);
                    }
                }

                break;
        }

        return result;
    }

    /// <summary>Every face touched by the current selection.</summary>
    public HashSet<int> ResolveFaces(Mesh mesh)
    {
        var result = new HashSet<int>();

        switch (Mode)
        {
            case ElementMode.Face:
                result.UnionWith(Faces);
                break;
            case ElementMode.Edge:
                foreach (var edge in Edges)
                {
                    result.UnionWith(MeshQueries.FacesOfEdge(mesh, edge));
                }

                break;
            case ElementMode.Vertex:
                foreach (var vertex in Vertices)
                {
                    for (var f = 0; f < mesh.Faces.Count; f++)
                    {
                        if (mesh.Faces[f].Contains(vertex))
                        {
                            result.Add(f);
                        }
                    }
                }

                break;
        }

        return result;
    }

    public EditSelection Clone()
    {
        var clone = new EditSelection { Mode = Mode };
        clone.Vertices.UnionWith(Vertices);
        clone.Edges.UnionWith(Edges);
        clone.Faces.UnionWith(Faces);
        return clone;
    }
}
