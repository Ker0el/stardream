using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StarrySky.Core.Math;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace StarrySky.Render;

/// <summary>Collects line segments into one buffer so the whole batch draws in a single call.</summary>
public sealed class LineBatch : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LineVertex
    {
        public float X, Y, Z;
        public float R, G, B, A;
    }

    public const int Stride = 28;

    private readonly List<LineVertex> _vertices = new();
    private ID3D11Buffer? _buffer;

    public int VertexCount => _vertices.Count;

    public void Clear() => _vertices.Clear();

    public void AddLine(Vec3 a, Vec3 b, Color4 color)
    {
        _vertices.Add(new LineVertex { X = a.X, Y = a.Y, Z = a.Z, R = color.R, G = color.G, B = color.B, A = color.A });
        _vertices.Add(new LineVertex { X = b.X, Y = b.Y, Z = b.Z, R = color.R, G = color.G, B = color.B, A = color.A });
    }

    /// <summary>
    /// Rebuilds an immutable vertex buffer with exactly the collected vertices.
    /// Line data is tiny and changes rarely, so recreating beats maintaining a
    /// dynamic buffer and the mapping path that goes with it.
    /// </summary>
    public void Upload(ID3D11Device device, ID3D11DeviceContext context)
    {
        _buffer?.Dispose();
        _buffer = null;

        if (_vertices.Count == 0)
        {
            return;
        }

        var data = _vertices.ToArray();
        var desc = new BufferDescription(
            (uint)(data.Length * Stride),
            BindFlags.VertexBuffer,
            ResourceUsage.Immutable);

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var initial = new SubresourceData(handle.AddrOfPinnedObject(), (uint)(data.Length * Stride));
            _buffer = device.CreateBuffer(desc, initial);
        }
        finally
        {
            handle.Free();
        }
    }

    public void Draw(ID3D11DeviceContext context)
    {
        if (_buffer is null || _vertices.Count == 0)
        {
            return;
        }

        context.IASetVertexBuffer(0, _buffer, Stride);
        context.Draw((uint)_vertices.Count, 0);
    }

    public void Dispose()
    {
        _buffer?.Dispose();
        _buffer = null;
    }
}
