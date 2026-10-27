using StarDream.Core.Editing;
using StarDream.Core.Math;
using StarDream.Core.Scene;
using Xunit;

namespace StarDream.Core.Tests;

public class GizmoTests
{
    private static readonly Vec3 ViewDirection = new Vec3(0, 0, -1);

    private static Ray RayAt(float x, float y)
        => new(new Vec3(x, y, 10f), ViewDirection);

    private static SceneObject Target()
    {
        var obj = SceneObject.FromPrimitive(PrimitiveParams.Cube(), "c");
        obj.Position = Vec3.Zero;
        return obj;
    }

    [Fact]
    public void Translate_X_Moves_Only_Along_X()
    {
        var target = Target();
        var drag = GizmoDrag.Begin(target, GizmoMode.Translate, GizmoAxis.X, RayAt(0f, 0f), ViewDirection, 1f);

        Assert.NotNull(drag);
        drag!.Update(RayAt(1f, 0f), ViewDirection);

        Assert.Equal(1f, target.Position.X, 3);
        Assert.Equal(0f, target.Position.Y, 3);
        Assert.Equal(0f, target.Position.Z, 3);
    }

    [Fact]
    public void Rotate_Z_By_Ninety_Degrees()
    {
        var target = Target();
        var drag = GizmoDrag.Begin(target, GizmoMode.Rotate, GizmoAxis.Z, RayAt(1f, 0f), ViewDirection, 1f);

        Assert.NotNull(drag);
        drag!.Update(RayAt(0f, 1f), ViewDirection);

        Assert.Equal(90f, target.Rotation.Z, 0);
    }

    [Fact]
    public void Scale_X_Doubles_When_Dragged_By_The_Gizmo_Length()
    {
        var target = Target();
        var drag = GizmoDrag.Begin(target, GizmoMode.Scale, GizmoAxis.X, RayAt(0f, 0f), ViewDirection, 1f);

        Assert.NotNull(drag);
        drag!.Update(RayAt(1f, 0f), ViewDirection);

        Assert.Equal(2f, target.Scale.X, 3);
        Assert.Equal(1f, target.Scale.Y, 3);
    }

    [Fact]
    public void Scale_Never_Reaches_Zero()
    {
        var target = Target();
        var drag = GizmoDrag.Begin(target, GizmoMode.Scale, GizmoAxis.X, RayAt(0f, 0f), ViewDirection, 1f);

        Assert.NotNull(drag);
        drag!.Update(RayAt(-5f, 0f), ViewDirection);

        Assert.True(target.Scale.X >= 0.01f);
    }

    [Fact]
    public void Parameter_On_Axis_Is_Null_When_The_Ray_Is_Parallel_To_The_Plane()
    {
        var parallel = new Ray(new Vec3(0, 5, 0), new Vec3(1, 0, 0));

        var parameter = GizmoMath.ParameterOnAxis(parallel, Vec3.Zero, Vec3.UnitX, Vec3.UnitX);

        Assert.Null(parameter);
    }
}
