using System;
using System.Runtime.InteropServices;
using StarrySky.Core.Math;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace StarrySky.Render;

/// <summary>Draws the ground grid and the world axes.</summary>
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
    private readonly LineBatch _grid = new();
    private readonly LineBatch _axes = new();

    private static readonly Color4 GridColor = new(0.21f, 0.22f, 0.24f, 1f);
    private static readonly Color4 GridCenterColor = new(0.33f, 0.34f, 0.37f, 1f);

    public SceneRenderer(Device device)
    {
        _device = device;

        _lineVertexShader = ShaderLibrary.CreateVertexShader(
            device.D3DDevice, "StarrySky.Render.Shaders.line.hlsl", "VSMain", out var bytecode);
        _linePixelShader = ShaderLibrary.CreatePixelShader(
            device.D3DDevice, "StarrySky.Render.Shaders.line.hlsl", "PSMain");

        _lineInputLayout = device.D3DDevice.CreateInputLayout(
            new[]
            {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
            },
            bytecode);

        _constantBuffer = device.D3DDevice.CreateBuffer(new BufferDescription(
            64, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));

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
    }

    public void DrawGrid(Mat4 viewProjection)
    {
        _grid.Clear();
        BuildGrid();


        var context = _device.Context;
        UploadConstants(context, viewProjection);

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
        UploadConstants(context, viewProjection);

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

    private static int _diagCount;

    private static void Diag(string message)
    {
        if (_diagCount >= 6)
        {
            return;
        }

        _diagCount++;
        try
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "starrysky-diag.txt"),
                message + Environment.NewLine);
        }
        catch (System.IO.IOException)
        {
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

    private void UploadConstants(ID3D11DeviceContext context, Mat4 m)
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

    public void Dispose()
    {
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
