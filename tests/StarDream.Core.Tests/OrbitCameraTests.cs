using System;
using StarDream.Core.Cameras;
using StarDream.Core.Math;
using Xunit;

namespace StarDream.Core.Tests;

public class OrbitCameraTests
{
    [Fact]
    public void Orbit_Clamps_Pitch_To_89_Degrees()
    {
        var camera = new OrbitCamera();

        camera.Orbit(0f, 10f);

        Assert.Equal(89f * MathUtil.DegToRad, camera.Pitch, 2);
    }

    [Fact]
    public void Dolly_Clamps_Distance_Between_0_5_And_200()
    {
        var camera = new OrbitCamera();

        camera.Dolly(0.0001f);
        Assert.Equal(0.5f, camera.Distance, 3);

        camera.Dolly(100000f);
        Assert.Equal(200f, camera.Distance, 3);
    }

    [Fact]
    public void Frame_Centers_Target_And_Fits_Radius()
    {
        var camera = new OrbitCamera();

        camera.Frame(new Vec3(1, 2, 3), 2f);

        Assert.Equal(new Vec3(1, 2, 3), camera.Target);
        Assert.True(camera.Distance > 2f);
        var expected = 2f / MathF.Sin(22.5f * MathUtil.DegToRad);
        Assert.Equal(expected, camera.Distance, 1);
    }

    [Fact]
    public void Pan_Moves_Target_Along_Camera_Right_Only()
    {
        var camera = new OrbitCamera();
        var before = camera.Target;
        var view = camera.GetView();
        var right = new Vec3(view.M11, view.M12, view.M13);

        camera.Pan(100f, 0f, 600);

        var delta = camera.Target - before;
        Assert.True(Vec3.Dot(delta, right) < 0f, "dragging right must move the target left along camera right");
        Assert.True(MathF.Abs(delta.Y) < 1e-3f, "a horizontal pan must not move the target vertically");
    }

    [Fact]
    public void Pan_Down_Moves_Target_Up()
    {
        var camera = new OrbitCamera();
        var before = camera.Target;

        camera.Pan(0f, 100f, 600);

        var delta = camera.Target - before;
        Assert.True(delta.Y > 0f, "dragging down must move the target up");
    }

    [Fact]
    public void ScreenToRay_From_Viewport_Center_Points_At_Target()
    {
        var camera = new OrbitCamera();
        camera.Frame(Vec3.Zero, 1f);

        var ray = camera.ScreenToRay(new Vec2(400, 300), 800, 600);
        var toTarget = (camera.Target - ray.Origin).Normalized();

        Assert.True(Vec3.Dot(ray.Direction, toTarget) > 0.999f);
    }
}
