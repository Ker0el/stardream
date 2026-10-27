using StarrySky.Core.Cameras;
using StarrySky.Core.Math;
using StarrySky.Core.Meshes;
using StarrySky.Core.Picking;
using StarrySky.Core.Scene;
using Xunit;

namespace StarrySky.Core.Tests;

public class ElementPickerTests
{
    private static Mat4 ViewProjection(out OrbitCamera camera)
    {
        camera = new OrbitCamera();
        camera.Target = Vec3.Zero;
        camera.Distance = 8f;
        camera.Yaw = 0f;
        camera.Pitch = 0f;
        return camera.GetProjection(16f / 9f) * camera.GetView();
    }

    [Fact]
    public void PickVertex_Finds_Vertex_Under_The_Cursor()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var viewProjection = ViewProjection(out var camera);
        var projected = (viewProjection * Mat4.Identity).TransformPoint(mesh.Positions[0]);
        var screen = new Vec2((projected.X * 0.5f + 0.5f) * 1600f, (1f - (projected.Y * 0.5f + 0.5f)) * 900f);

        var picked = ElementPicker.PickVertex(mesh, Mat4.Identity, viewProjection, screen, 1600, 900);

        Assert.NotNull(picked);
        Assert.Equal(0, picked.Value);
    }

    [Fact]
    public void PickVertex_Returns_Null_When_Off_Any_Vertex()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var viewProjection = ViewProjection(out _);

        var picked = ElementPicker.PickVertex(mesh, Mat4.Identity, viewProjection, new Vec2(5f, 5f), 1600, 900);

        Assert.Null(picked);
    }

    [Fact]
    public void PickEdge_Finds_The_Nearest_Edge()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var viewProjection = ViewProjection(out _);
        var transform = viewProjection * Mat4.Identity;
        var a = transform.TransformPoint(mesh.Positions[0]);
        var b = transform.TransformPoint(mesh.Positions[1]);
        var mid = new Vec2(
            ((a.X + b.X) * 0.5f * 0.5f + 0.5f) * 1600f,
            (1f - ((a.Y + b.Y) * 0.5f * 0.5f + 0.5f)) * 900f);

        var picked = ElementPicker.PickEdge(mesh, Mat4.Identity, viewProjection, mid, 1600, 900);

        Assert.NotNull(picked);
    }

    [Fact]
    public void PickFace_Returns_The_Face_Under_The_Ray()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var camera = new OrbitCamera();
        camera.Target = Vec3.Zero;
        camera.Distance = 5f;
        camera.Yaw = 0f;
        camera.Pitch = 1.5f;
        var ray = camera.ScreenToRay(new Vec2(800, 450), 1600, 900);

        var picked = ElementPicker.PickFace(mesh, Mat4.Identity, ray);

        Assert.Equal(0, picked);
    }

    [Fact]
    public void PickFace_Returns_Null_When_The_Ray_Misses()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());

        var picked = ElementPicker.PickFace(mesh, Mat4.Identity, new Ray(new Vec3(50, 50, 50), new Vec3(0, 0, -1)));

        Assert.Null(picked);
    }
}
