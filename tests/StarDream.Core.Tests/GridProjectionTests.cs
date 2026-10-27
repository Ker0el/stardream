using StarDream.Core.Cameras;
using StarDream.Core.Math;
using Xunit;

namespace StarDream.Core.Tests;

public class GridProjectionTests
{
    [Fact]
    public void Default_Camera_Sees_Most_Of_The_Ground_Grid()
    {
        var camera = new OrbitCamera();
        var viewProjection = camera.GetProjection(1920f / 1023f) * camera.GetView();

        var visibleLines = 0;
        for (var i = -10; i <= 10; i++)
        {
            if (LineIsVisible(viewProjection, new Vec3(i, 0, -10), new Vec3(i, 0, 10)))
            {
                visibleLines++;
            }

            if (LineIsVisible(viewProjection, new Vec3(-10, 0, i), new Vec3(10, 0, i)))
            {
                visibleLines++;
            }
        }

        Assert.True(visibleLines >= 20, $"only {visibleLines} grid lines project into the view");
    }

    [Fact]
    public void Origin_Projects_To_The_Center_Of_The_View()
    {
        var camera = new OrbitCamera();
        var viewProjection = camera.GetProjection(16f / 9f) * camera.GetView();

        var origin = viewProjection.TransformPoint(Vec3.Zero);

        Assert.InRange(origin.X, -0.15f, 0.15f);
        Assert.InRange(origin.Y, -0.6f, 0.6f);
    }

    [Fact]
    public void Camera_Looks_Down_At_The_Ground()
    {
        var camera = new OrbitCamera();

        Assert.True(camera.Eye.Y > camera.Target.Y);
        Assert.True(camera.Pitch > 0f);
    }

    private static bool LineIsVisible(Mat4 viewProjection, Vec3 a, Vec3 b)
    {
        var steps = 32;
        for (var s = 0; s <= steps; s++)
        {
            var p = viewProjection.TransformPoint(Vec3.Lerp(a, b, s / (float)steps));
            if (p.X >= -1f && p.X <= 1f && p.Y >= -1f && p.Y <= 1f && p.Z >= 0f && p.Z <= 1f)
            {
                return true;
            }
        }

        return false;
    }
}
