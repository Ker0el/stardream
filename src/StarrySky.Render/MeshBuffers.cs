using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StarrySky.Core.Meshes;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace StarrySky.Render;

/// <summary>Uploads mesh corner data to GPU buffers and caches them per mesh instance.</summary>
public sealed class MeshBuffers : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SolidVertex
    {
        public float Px, Py, Pz;
        public float Nx, Ny, Nz;
    }

    public const int Stride = 24;

    private sealed class Entry
    {
        public ID3D11Buffer Buffer = null!;
        public uint Count;
    }

    private readonly Dictionary<Mesh, Entry> _cache = new();

    public ID3D11Buffer GetBuffer(ID3D11Device device, Mesh mesh, out uint vertexCount)
    {
        if (_cache.TryGetValue(mesh, out var entry))
        {
            vertexCount = entry.Count;
            return entry.Buffer;
        }

        var vertices = BuildVertices(mesh);
        var desc = new BufferDescription(
            (uint)(vertices.Length * Stride),
            BindFlags.VertexBuffer,
            ResourceUsage.Immutable);

        var handle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        try
        {
            var initial = new SubresourceData(handle.AddrOfPinnedObject(), (uint)(vertices.Length * Stride));
            entry = new Entry
            {
                Buffer = device.CreateBuffer(desc, initial),
                Count = (uint)vertices.Length,
            };
        }
        finally
        {
            handle.Free();
        }

        _cache[mesh] = entry;
        vertexCount = entry.Count;
        return entry.Buffer;
    }

    public void Invalidate(Mesh mesh)
    {
        if (_cache.Remove(mesh, out var entry))
        {
            entry.Buffer.Dispose();
        }
    }

    private static SolidVertex[] BuildVertices(Mesh mesh)
    {
        var triangles = mesh.Triangles.Count;
        var vertices = new SolidVertex[triangles * 3];
        var write = 0;

        for (var f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            var offset = mesh.CornerOffset(f);

            for (var i = 1; i < face.Length - 1; i++)
            {
                Append(ref write, vertices, mesh, face[0], offset + 0);
                Append(ref write, vertices, mesh, face[i], offset + i);
                Append(ref write, vertices, mesh, face[i + 1], offset + i + 1);
            }
        }

        if (write != vertices.Length)
        {
            Array.Resize(ref vertices, write);
        }

        return vertices;
    }

    private static void Append(ref int write, SolidVertex[] target, Mesh mesh, int positionIndex, int cornerIndex)
    {
        var position = mesh.Positions[positionIndex];
        var normal = cornerIndex < mesh.CornerNormals.Length
            ? mesh.CornerNormals[cornerIndex]
            : mesh.FaceNormals[0];

        target[write++] = new SolidVertex
        {
            Px = position.X,
            Py = position.Y,
            Pz = position.Z,
            Nx = normal.X,
            Ny = normal.Y,
            Nz = normal.Z,
        };
    }

    public void Dispose()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Buffer.Dispose();
        }

        _cache.Clear();
    }
}
