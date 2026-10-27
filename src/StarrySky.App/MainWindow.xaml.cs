using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StarrySky.Core.Scene;
using StarrySky.Core.Math;

namespace StarrySky.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
        LoadDemoScene();
    }

    /// <summary>Temporary content until objects can be added from the toolbar.</summary>
    private void LoadDemoScene()
    {
        var demo = DemoScene.Create();
        var document = ViewportHost.Document;

        foreach (var source in demo.Objects)
        {
            var target = document.AddPrimitive(source.Primitive ?? PrimitiveParams.Cube());
            target.Position = source.Position;
            target.ColorHex = source.ColorHex;
            target.Name = source.Name;
        }

        document.Select(null);
        document.Camera.Target = new Vec3(0f, 0.5f, 0f);
        document.Camera.Yaw = 0.6f;
        document.Camera.Pitch = 0.4f;
        document.Camera.Distance = 12f;
        document.IsDirty = false;
    }
}
