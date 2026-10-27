using StarDream.Core.Math;

namespace StarDream.Core.Scene;

/// <summary>A small starter scene: cube, sphere, cylinder and a ground plane.</summary>
public static class DemoScene
{
    public static SceneDocument Create()
    {
        var document = new SceneDocument();

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
        return document;
    }
}
