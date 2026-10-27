using System;
using System.Runtime.InteropServices;
using StarrySky.Core.Math;
using StarrySky.Core.Scene;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace StarrySky.Render;

/// <summary>Draws the ground grid, the world axes and every visible scene object.</summary>
public sealed class SceneRenderer : IDisposable
{
    private const float GridExtent = 10f;
    private const float GridStep = 1f;

    private readonly Device _device;
    private readonly ID3D11VertexShader _lineVertexShader;
    private readonly ID3D11PixelShader _linePixelShader;
    private readonly ID3D11InputLayout _lineInputLayout;
    private readonly ID3D11Buffer _constantBuffer;
    private readonly ID3D11DepthStencilState _depthTested;
    private readonly ID3D11DepthStencilState _depthDisabled;
    private readonly ID3D11RasterizerState _rasterizer;
    private readonly LineBatch _grid = new();
    private readonly LineBatch _axes = new();

    private readonly ID3D11VertexShader _solidVertexShader;
    private readonly ID3D11PixelShader _solidPixelShader;
    private readonly ID3D11InputLayout _solidInputLayout;
    private readonly ID3D11Buffer _solidConstants;
    private readonly MeshBuffers _meshBuffers = new();

    private static readonly Color4 GridColor = new(0.21f, 0.22f, 0.24f, 1f);
    private static readonly Color4 GridCenterColor = new(0.33f, 0.34f, 0.37f, 1f);

    public SceneRenderer(Device device)
    {
        _device = device;

        _lineVertexShader = ShaderLibrary.CreateVertexShader(
            device.D3DDevice, "StarrySky.Render.Shaders.line.hlsl", "VSMain", out var lineBytecode);
        _linePixelShader = ShaderLibrary.CreatePixelShader(
            device.D3DDevice, "StarrySky.Render.Shaders.line.hlsl", "PSMain");

        _lineInputLayout = device.D3DDevice.CreateInputLayout(
            new[]
            {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
            },
            lineBytecode);

        _constantBuffer = device.D3DDevice.CreateBuffer(new BufferDescription(
            64, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));

        _solidVertexShader = ShaderLibrary.CreateVertexShader(
            device.D3DDevice, "StarrySky.Render.Shaders.solid.hlsl", "VSMain", out var solidBytecode);
        _solidPixelShader = ShaderLibrary.CreatePixelShader(
            device.D3DDevice, "StarrySky.Render.Shaders.solid.hlsl", "PSMain");

        _solidInputLayout = device.D3DDevice.CreateInputLayout(
            new[]
            {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            },
            solidBytecode);

        _solidConstants = device.D3DDevice.CreateBuffer(new BufferDescription(
            144, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));

        _depthTested = device.D3DDevice.CreateDepthStencilState(new DepthStencilDescription
        {
            DepthEnable = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthFunc = ComparisonFunction.LessEqual,
        });

        _depthDisabled = device.D3DDevice.CreateDepthStencilState(new DepthStencilDescription
        {
            DepthEnable = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Always,
        });

        _rasterizer = device.D3DDevice.CreateRasterizerState(new RasterizerDescription
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            DepthClipEnable = true,
        });
    }

    public void DrawGrid(Mat4 viewProjection)
    {
        _grid.Clear();
        BuildGrid();

        var context = _device.Context;
        UploadLineConstants(context, viewProjection);

        context.IASetInputLayout(_lineInputLayout);
        context.IASetPrimitiveTopology(PrimitiveTopology.LineList);
        context.VSSetShader(_lineVertexShader);
        context.PSSetShader(_linePixelShader);
        context.VSSetConstantBuffer(0, _constantBuffer);

        context.OMSetDepthStencilState(_depthTested);
        _grid.Upload(_device.D3DDevice, context);
        _grid.Draw(context);
    }

    /// <summary>The axes ignore depth so they stay visible against the ground grid.</summary>
    public void DrawAxes(Mat4 viewProjection)
    {
        _axes.Clear();
        BuildAxes();

        var context = _device.Context;
        UploadLineConstants(context, viewProjection);

        context.IASetInputLayout(_lineInputLayout);
        context.IASetPrimitiveTopology(PrimitiveTopology.LineList);
        context.VSSetShader(_lineVertexShader);
        context.PSSetShader(_linePixelShader);
        context.VSSetConstantBuffer(0, _constantBuffer);

        context.OMSetDepthStencilState(_depthDisabled);
        _axes.Upload(_device.D3DDevice, context);
        _axes.Draw(context);
        context.OMSetDepthStencilState(_depthTested);
    }

    public void DrawMeshes(SceneDocument document, Mat4 viewProjection)
    {
        var context = _device.Context;

        context.IASetInputLayout(_solidInputLayout);
        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.VSSetShader(_solidVertexShader);
        context.PSSetShader(_solidPixelShader);
        context.VSSetConstantBuffer(0, _solidConstants);
        context.PSSetConstantBuffer(0, _solidConstants);
        context.OMSetDepthStencilState(_depthTested);
        context.RSSetState(_rasterizer);

        foreach (var obj in document.Objects)
        {
            if (!obj.Visible)
            {
                continue;
            }

            var mesh = obj.GetMesh();
            if (mesh.Triangles.Count == 0)
            {
                continue;
            }

            var buffer = _meshBuffers.GetBuffer(_device.D3DDevice, mesh, out var vertexCount);
            UploadSolidConstants(context, obj.LocalMatrix(), viewProjection, obj.Color);

            context.IASetVertexBuffer(0, buffer, MeshBuffers.Stride);
            context.Draw(vertexCount, 0);
        }
    }

    private void BuildGrid()
    {
        for (var i = -GridExtent; i <= GridExtent; i += GridStep)
        {
            var color = MathF.Abs(i) < 0.001f ? GridCenterColor : GridColor;
            _grid.AddLine(new Vec3(i, 0, -GridExtent), new Vec3(i, 0, GridExtent), color);
            _grid.AddLine(new Vec3(-GridExtent, 0, i), new Vec3(GridExtent, 0, i), color);
        }
    }

    private void BuildAxes()
    {
        const float length = 3f;
        _axes.AddLine(Vec3.Zero, new Vec3(length, 0, 0), new Color4(0.9f, 0.25f, 0.25f, 1f));
        _axes.AddLine(Vec3.Zero, new Vec3(0, length, 0), new Color4(0.3f, 0.95f, 0.35f, 1f));
        _axes.AddLine(Vec3.Zero, new Vec3(0, 0, length), new Color4(0.25f, 0.55f, 1f, 1f));
    }

    private void UploadLineConstants(ID3D11DeviceContext context, Mat4 m)
    {
        var data = new float[16]
        {
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44,
        };

        var mapped = context.Map(_constantBuffer, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        try
        {
            Marshal.Copy(data, 0, mapped.DataPointer, data.Length);
        }
        finally
        {
            context.Unmap(_constantBuffer, 0);
        }
    }

    private void UploadSolidConstants(ID3D11DeviceContext context, Mat4 world, Mat4 viewProjection, Vec3 color)
    {
        var data = new float[36]
        {
            world.M11, world.M12, world.M13, world.M14,
            world.M21, world.M22, world.M23, world.M24,
            world.M31, world.M32, world.M33, world.M34,
            world.M41, world.M42, world.M43, world.M44,

            viewProjection.M11, viewProjection.M12, viewProjection.M13, viewProjection.M14,
            viewProjection.M21, viewProjection.M22, viewProjection.M23, viewProjection.M24,
            viewProjection.M31, viewProjection.M32, viewProjection.M33, viewProjection.M34,
            viewProjection.M41, viewProjection.M42, viewProjection.M43, viewProjection.M44,

            color.X, color.Y, color.Z, 1f,
        };

        var mapped = context.Map(_solidConstants, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        try
        {
            Marshal.Copy(data, 0, mapped.DataPointer, data.Length);
        }
        finally
        {
            context.Unmap(_solidConstants, 0);
        }
    }

    public void Dispose()
    {
        _meshBuffers.Dispose();
        _rasterizer.Dispose();
        _solidConstants.Dispose();
        _solidInputLayout.Dispose();
        _solidPixelShader.Dispose();
        _solidVertexShader.Dispose();
        _grid.Dispose();
        _axes.Dispose();
        _depthDisabled.Dispose();
        _depthTested.Dispose();
        _constantBuffer.Dispose();
        _lineInputLayout.Dispose();
        _linePixelShader.Dispose();
        _lineVertexShader.Dispose();
    }
}
