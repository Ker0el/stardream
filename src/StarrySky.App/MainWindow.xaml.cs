using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StarrySky.Core.Math;
using StarrySky.Core.Scene;

namespace StarrySky.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new DesktopAcrylicBackdrop();
        PopulateDemoScene();
    }

    /// <summary>Temporary content until objects can be added from the toolbar.</summary>
    private void PopulateDemoScene()
    {
        var document = ViewportHost.Document;

        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Position = new Vec3(-2f, 0.5f, 0f);
        cube.ColorHex = "#C9C9CE";

        var sphere = document.AddPrimitive(PrimitiveParams.Sphere());
        sphere.Position = new Vec3(0f, 0.5f, 0f);
        sphere.ColorHex = "#B9C6D8";

        var cylinder = document.AddPrimitive(PrimitiveParams.Cylinder());
        cylinder.Position = new Vec3(2f, 0.5f, 0f);
        cylinder.ColorHex = "#CDBBA0";

        var plane = document.AddPrimitive(PrimitiveParams.Plane(6f, 4f));
        plane.Position = new Vec3(0f, 0f, 2.5f);
        plane.ColorHex = "#9FA6B0";

        document.Select(null);
        document.IsDirty = false;
    }
}
