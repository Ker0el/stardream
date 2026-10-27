using StarrySky.Core.Math;
using StarrySky.Core.Scene;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;

namespace StarrySky.Render;

/// <summary>
/// Owns the composition swap chain, its render target and depth buffer.
/// Bind <see cref="SwapChain"/> to a SwapChainPanel through ISwapChainPanelNative.
/// </summary>
public sealed class ViewportRenderer : IDisposable
{
    // #1E1F22 鈥?the viewport background, matched to the dark UI.
    private static readonly Color4 ClearColor = new(0.1176f, 0.1216f, 0.1333f, 1f);

    private readonly Device _device;
    private readonly SceneRenderer _sceneRenderer;
    private IDXGISwapChain1? _swapChain;
    private ID3D11RenderTargetView? _renderTarget;
    private ID3D11Texture2D? _depthTexture;
    private ID3D11DepthStencilView? _depthView;
    private int _width;
    private int _height;

    public ViewportRenderer(Device device, int width, int height)
    {
        _device = device;
        _sceneRenderer = new SceneRenderer(device);
        CreateSwapChain(Math.Max(1, width), Math.Max(1, height));
        CreateSizeDependentResources(Math.Max(1, width), Math.Max(1, height));
    }

    public IDXGISwapChain1? SwapChain => _swapChain;

    private void CreateSwapChain(int width, int height)
    {
        using var dxgiDevice = _device.D3DDevice.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetAdapter();
        using var factory = adapter.GetParent<IDXGIFactory2>();

        var desc = new SwapChainDescription1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Premultiplied,
        };

        _swapChain = factory.CreateSwapChainForComposition(_device.D3DDevice, desc);
    }

    private void CreateSizeDependentResources(int width, int height)
    {
        _renderTarget?.Dispose();
        _depthView?.Dispose();
        _depthTexture?.Dispose();

        using var backBuffer = _swapChain!.GetBuffer<ID3D11Texture2D>(0);
        _renderTarget = _device.D3DDevice.CreateRenderTargetView(backBuffer);

        var depthDesc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.D24_UNorm_S8_UInt,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None,
        };

        _depthTexture = _device.D3DDevice.CreateTexture2D(depthDesc);
        _depthView = _device.D3DDevice.CreateDepthStencilView(_depthTexture);

        _width = width;
        _height = height;
    }

    /// <summary>
    /// Resizes the swap chain. Width or height of zero (minimised / collapsed panel)
    /// is ignored on purpose: resizing to zero would fail and leave the surface unbound.
    /// </summary>
    public void Resize(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return;
        }

        if (pixelWidth == _width && pixelHeight == _height)
        {
            return;
        }

        _device.Context.OMSetRenderTargets(Array.Empty<ID3D11RenderTargetView>(), null);
        CreateSizeDependentResources(pixelWidth, pixelHeight);
        _swapChain!.ResizeBuffers(2, (uint)pixelWidth, (uint)pixelHeight, Format.B8G8R8A8_UNorm, SwapChainFlags.None);
    }

    public void Render(SceneDocument document)
    {
        if (_swapChain is null || _renderTarget is null || _depthView is null)
        {
            return;
        }

        var aspect = _height > 0 ? (float)_width / _height : 1f;
        var view = document.Camera.GetView();
        var projection = document.Camera.GetProjection(aspect);
        var viewProjection = projection * view;

        var context = _device.Context;
        context.RSSetViewport(new Viewport(0f, 0f, _width, _height, 0f, 1f));
        context.OMSetRenderTargets(_renderTarget, _depthView);
        context.ClearRenderTargetView(_renderTarget, ClearColor);
        context.ClearDepthStencilView(_depthView, DepthStencilClearFlags.Depth, 1f, 0);
        _sceneRenderer.DrawMeshes(document, viewProjection);
        _sceneRenderer.DrawSelectionWires(document, viewProjection);
        _sceneRenderer.DrawGrid(viewProjection);
        _sceneRenderer.DrawAxes(viewProjection);
        _swapChain.Present(1, PresentFlags.None);
    }

    public void Dispose()
    {
        _sceneRenderer.Dispose();
        _renderTarget?.Dispose();
        _depthView?.Dispose();
        _depthTexture?.Dispose();
        _swapChain?.Dispose();
    }
}

