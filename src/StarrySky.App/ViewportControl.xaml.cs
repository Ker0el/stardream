using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StarrySky.Core.Cameras;
using StarrySky.Render;

namespace StarrySky.App;

public sealed partial class ViewportControl : UserControl
{
    [ComImport]
    [Guid("63AAD0B8-7C24-40FF-85A8-640D944CC325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISwapChainPanelNative
    {
        [PreserveSig]
        int SetSwapChain(IntPtr swapChain);
    }

    private Device? _device;
    private ViewportRenderer? _renderer;
    private bool _renderingHooked;

    public OrbitCamera Camera { get; } = new();

    public ViewportControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Panel.SizeChanged += OnPanelSizeChanged;
        Panel.CompositionScaleChanged += OnCompositionScaleChanged;
    }

    public ViewportRenderer? Renderer => _renderer;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_device is not null)
        {
            return;
        }

        var (width, height) = PixelSize();
        _device = new Device();
        _renderer = new ViewportRenderer(_device, width, height);

        if (_renderer.SwapChain is not null)
        {
            AttachSwapChain(Panel, _renderer.SwapChain.NativePointer);
        }

        if (!_renderingHooked)
        {
            CompositionTarget.Rendering += OnRendering;
            _renderingHooked = true;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_renderingHooked)
        {
            CompositionTarget.Rendering -= OnRendering;
            _renderingHooked = false;
        }

        _renderer?.Dispose();
        _renderer = null;
        _device?.Dispose();
        _device = null;
    }

    private void OnRendering(object? sender, object e)
    {
        if (_renderer is null)
        {
            return;
        }

        var (width, height) = PixelSize();
        _renderer.Resize(width, height);
        var aspect = height > 0 ? (float)width / height : 1f;
        _renderer.Render(Camera.GetView(), Camera.GetProjection(aspect));
    }

    private void OnPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_renderer is null)
        {
            return;
        }

        var (width, height) = PixelSize();
        _renderer.Resize(width, height);
    }

    private void OnCompositionScaleChanged(SwapChainPanel sender, object args)
    {
        if (_renderer is null)
        {
            return;
        }

        var (width, height) = PixelSize();
        _renderer.Resize(width, height);
    }

    private (int Width, int Height) PixelSize()
    {
        var scaleX = Panel.CompositionScaleX > 0 ? Panel.CompositionScaleX : 1.0;
        var scaleY = Panel.CompositionScaleY > 0 ? Panel.CompositionScaleY : 1.0;
        var width = (int)Math.Round(Math.Max(1.0, Panel.ActualWidth * scaleX));
        var height = (int)Math.Round(Math.Max(1.0, Panel.ActualHeight * scaleY));
        return (width, height);
    }

    private static void AttachSwapChain(SwapChainPanel panel, IntPtr swapChain)
    {
        var unknown = Marshal.GetIUnknownForObject(panel);
        var iid = typeof(ISwapChainPanelNative).GUID;
        try
        {
            if (Marshal.QueryInterface(unknown, ref iid, out var nativePtr) != 0)
            {
                throw new InvalidOperationException("SwapChainPanel does not expose ISwapChainPanelNative.");
            }

            try
            {
                var native = (ISwapChainPanelNative)Marshal.GetObjectForIUnknown(nativePtr);
                native.SetSwapChain(swapChain);
            }
            finally
            {
                Marshal.Release(nativePtr);
            }
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }
}
