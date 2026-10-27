using StarrySky.Core.Math;
using StarrySky.Core.Picking;
using StarrySky.Core.Scene;
using Xunit;

namespace StarrySky.Core.Tests;

public class RayPickerTests
{
    [Fact]
    public void Ray_Through_Cube_Center_Hits_Front_Face()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Position = Vec3.Zero;

        var hit = RayPicker.PickObject(document, new Ray(new Vec3(0, 0, 5), new Vec3(0, 0, -1)));

        Assert.Same(cube, hit);
    }

    [Fact]
    public void Ray_Missing_The_Scene_Returns_Null()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Position = Vec3.Zero;

        var hit = RayPicker.PickObject(document, new Ray(new Vec3(5, 5, 5), new Vec3(0, 0, -1)));

        Assert.Null(hit);
    }

    [Fact]
    public void Pick_Respects_Object_Transform()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Position = new Vec3(5, 0, 0);

        var miss = RayPicker.PickObject(document, new Ray(new Vec3(0, 0, 5), new Vec3(0, 0, -1)));
        var hit = RayPicker.PickObject(document, new Ray(new Vec3(5, 0, 5), new Vec3(0, 0, -1)));

        Assert.Null(miss);
        Assert.Same(cube, hit);
    }

    [Fact]
    public void Pick_Skips_Invisible_Objects()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Position = Vec3.Zero;
        cube.Visible = false;

        var hit = RayPicker.PickObject(document, new Ray(new Vec3(0, 0, 5), new Vec3(0, 0, -1)));

        Assert.Null(hit);
    }

    [Fact]
    public void RayTriangle_Returns_Entry_Distance()
    {
        var hit = RayPicker.RayTriangle(
            new Ray(new Vec3(0.25f, 0.25f, 1f), new Vec3(0, 0, -1)),
            new Vec3(0, 0, 0),
            new Vec3(1, 0, 0),
            new Vec3(0, 1, 0),
            out var distance);

        Assert.True(hit);
        Assert.Equal(1f, distance, 3);
    }

    [Fact]
    public void RayTriangle_Misses_Outside_The_Triangle()
    {
        var hit = RayPicker.RayTriangle(
            new Ray(new Vec3(0.8f, 0.8f, 1f), new Vec3(0, 0, -1)),
            new Vec3(0, 0, 0),
            new Vec3(1, 0, 0),
            new Vec3(0, 1, 0),
            out _);

        Assert.False(hit);
    }
}
