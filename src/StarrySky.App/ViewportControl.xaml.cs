using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using StarrySky.Core.Cameras;
using StarrySky.Core.Math;
using StarrySky.Core.Picking;
using StarrySky.Core.Scene;
using StarrySky.Render;
using Windows.System;

namespace StarrySky.App;

public sealed partial class ViewportControl : UserControl
{
    private enum DragMode
    {
        None,
        Orbit,
        Pan,
        Dolly,
    }

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

    private DragMode _drag = DragMode.None;
    private double _lastX;
    private double _lastY;
    private double _pressX;
    private double _pressY;

    public SceneDocument Document { get; } = new();
    public OrbitCamera Camera => Document.Camera;

    public ViewportControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Panel.SizeChanged += OnPanelSizeChanged;
        Panel.CompositionScaleChanged += OnCompositionScaleChanged;

        // Maya navigation: Alt + left tumbles, Alt + middle tracks, Alt + right dollies.
        InputLayer.PointerPressed += OnPointerPressed;
        InputLayer.PointerMoved += OnPointerMoved;
        InputLayer.PointerReleased += OnPointerReleased;
        InputLayer.PointerWheelChanged += OnPointerWheelChanged;
    }

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

        try
        {
            var (width, height) = PixelSize();
            _renderer.Resize(width, height);
            _renderer.Render(Document);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or ObjectDisposedException)
        {
            // A frame can land while the compositor is tearing the surface down
            // (minimise, DPI change, window swap). Skip it instead of killing the app.
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(InputLayer);
        _lastX = point.Position.X;
        _lastY = point.Position.Y;
        _pressX = _lastX;
        _pressY = _lastY;

        var alt = (e.KeyModifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu;
        if (alt)
        {
            var properties = point.Properties;
            _drag = properties.IsRightButtonPressed ? DragMode.Dolly
                : properties.IsMiddleButtonPressed ? DragMode.Pan
                : DragMode.Orbit;
        }
        else
        {
            _drag = DragMode.None;
        }

        Panel.CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(InputLayer);
        var x = point.Position.X;
        var y = point.Position.Y;

        if (_drag != DragMode.None)
        {
            var dx = x - _lastX;
            var dy = y - _lastY;

            switch (_drag)
            {
                case DragMode.Orbit:
                    Camera.Orbit((float)(-dx * 0.008), (float)(dy * 0.008));
                    break;
                case DragMode.Pan:
                    Camera.Pan((float)dx, (float)dy, (int)Math.Max(1, Panel.ActualHeight));
                    break;
                case DragMode.Dolly:
                    Camera.Dolly((float)Math.Exp(dy * 0.01));
                    break;
            }
        }

        _lastX = x;
        _lastY = y;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var moved = Math.Abs(_lastX - _pressX) + Math.Abs(_lastY - _pressY);
        var wasCameraDrag = _drag != DragMode.None;
        _drag = DragMode.None;
        Panel.ReleasePointerCapture(e.Pointer);

        if (wasCameraDrag || moved > 3)
        {
            return;
        }

        var (width, height) = PixelSize();
        var ray = Camera.ScreenToRay(
            new Vec2((float)(_pressX * Panel.CompositionScaleX), (float)(_pressY * Panel.CompositionScaleY)),
            width,
            height);

        Document.Select(RayPicker.PickObject(Document, ray));
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(InputLayer).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        Camera.Dolly(delta > 0 ? 0.9f : 1.1f);
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
