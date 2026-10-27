using StarDream.Core.Math;
using Xunit;

namespace StarDream.Core.Tests;

public class MathTests
{
    [Fact]
    public void FromTrs_Applies_Scale_Then_RotateZ_Then_Translate()
    {
        var m = Mat4.FromTrs(
            new Vec3(10, 0, 0),
            new Vec3(0, 0, 90),
            new Vec3(2, 1, 1));

        var p = m.TransformPoint(new Vec3(1, 0, 0));

        Assert.Equal(10f, p.X, 3);
        Assert.Equal(2f, p.Y, 3);
        Assert.Equal(0f, p.Z, 3);
    }

    [Fact]
    public void FromTrs_Rejects_NaN_At_Entry()
    {
        Assert.Throws<ArgumentException>(() =>
            Mat4.FromTrs(new Vec3(float.NaN, 0, 0), Vec3.Zero, Vec3.One));
    }

    [Fact]
    public void LookAt_Looks_Down_NegativeZ_From_Origin()
    {
        var view = Mat4.LookAt(Vec3.Zero, new Vec3(0, 0, -1), Vec3.UnitY);

        var p = view.TransformPoint(new Vec3(0, 0, -1));

        Assert.Equal(0f, p.X, 3);
        Assert.Equal(0f, p.Y, 3);
        Assert.Equal(-1f, p.Z, 3);
    }

    [Fact]
    public void PerspectiveFov_Maps_Near_And_Far_To_Zero_And_One()
    {
        var proj = Mat4.PerspectiveFov(45f * MathUtil.DegToRad, 16f / 9f, 0.1f, 500f);

        var near = proj.TransformPoint(new Vec3(0, 0, -0.1f));
        var far = proj.TransformPoint(new Vec3(0, 0, -500f));

        Assert.Equal(0f, near.Z, 3);
        Assert.Equal(1f, far.Z, 3);
    }

    [Fact]
    public void Invert_Undoes_A_Trs()
    {
        var m = Mat4.FromTrs(new Vec3(3, -2, 5), new Vec3(20, 35, -10), new Vec3(2, 2, 2));
        Assert.True(m.TryInvert(out var inv));

        var p = new Vec3(1.5f, -0.25f, 2f);
        var round = inv.TransformPoint(m.TransformPoint(p));

        Assert.Equal(p.X, round.X, 3);
        Assert.Equal(p.Y, round.Y, 3);
        Assert.Equal(p.Z, round.Z, 3);
    }

    [Fact]
    public void Vec2_Normalized_Of_Zero_Is_Zero()
    {
        Assert.Equal(Vec2.Zero, Vec2.Zero.Normalized());
    }
}
