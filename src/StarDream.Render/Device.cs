using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace StarDream.Render;

/// <summary>Owns the D3D11 device and immediate context shared by every renderer.</summary>
public sealed class Device : IDisposable
{
    public ID3D11Device D3DDevice { get; }
    public ID3D11DeviceContext Context { get; }

    public Device()
    {
        var levels = new[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0 };
        var result = D3D11.D3D11CreateDevice(
            adapter: null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            levels,
            out ID3D11Device device,
            out ID3D11DeviceContext context);

        if (result.Failure)
        {
            result = D3D11.D3D11CreateDevice(
                adapter: null,
                DriverType.Warp,
                DeviceCreationFlags.BgraSupport,
                levels,
                out device,
                out context);
        }

        result.CheckError();
        D3DDevice = device;
        Context = context;
    }

    public void Dispose()
    {
        Context.Dispose();
        D3DDevice.Dispose();
    }
}
