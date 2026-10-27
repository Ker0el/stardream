using StarrySky.Core.Math;
using StarrySky.Core.Meshes;
using StarrySky.Core.Scene;
using Xunit;

namespace StarrySky.Core.Tests;

public class NormalSmootherTests
{
    [Fact]
    public void Cube_All_Corners_Of_A_Face_Use_That_Face_Normal()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        NormalSmoother.Compute(mesh);

        for (var f = 0; f < mesh.Faces.Count; f++)
        {
            var offset = mesh.CornerOffset(f);
            for (var c = 0; c < mesh.Faces[f].Length; c++)
            {
                var normal = mesh.CornerNormals[offset + c];
                Assert.True(Vec3.Dot(normal, mesh.FaceNormals[f]) > 0.999f,
                    $"corner {c} of face {f} is not flat shaded");
            }
        }
    }

    [Fact]
    public void Sphere_Corner_Normals_Are_Radial()
    {
        var mesh = Primitives.Build(PrimitiveParams.Sphere(1f, 16, 8));
        NormalSmoother.Compute(mesh);

        for (var f = 0; f < mesh.Faces.Count; f++)
        {
            var offset = mesh.CornerOffset(f);
            for (var c = 0; c < mesh.Faces[f].Length; c++)
            {
                var position = mesh.Positions[mesh.Faces[f][c]];
                if (position.Length < 0.5f)
                {
                    continue; // poles: the normal is not radial by construction
                }

                var normal = mesh.CornerNormals[offset + c];
                Assert.True(Vec3.Dot(normal, position.Normalized()) > 0.95f,
                    $"corner normal at {position} is not radial");
            }
        }
    }

    [Fact]
    public void Faces_Under_Thirty_Degrees_Share_A_Normal()
    {
        var mesh = TwoFacesAtAngle(20f);
        NormalSmoother.Compute(mesh);

        var normalInFirst = mesh.CornerNormals[0];
        var normalInSecond = mesh.CornerNormals[mesh.CornerOffset(1)];

        Assert.True(Vec3.Dot(normalInFirst, normalInSecond) > 0.999f);
    }

    [Fact]
    public void Faces_Over_Thirty_Degrees_Do_Not_Share_A_Normal()
    {
        var mesh = TwoFacesAtAngle(45f);
        NormalSmoother.Compute(mesh);

        var normalInFirst = mesh.CornerNormals[0];
        var normalInSecond = mesh.CornerNormals[mesh.CornerOffset(1)];

        Assert.True(Vec3.Dot(normalInFirst, normalInSecond) < 0.99f);
    }

    private static Mesh TwoFacesAtAngle(float degrees)
    {
        var radians = degrees * MathUtil.DegToRad;
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);

        var mesh = new Mesh();
        mesh.Positions.Add(new Vec3(0, 0, 0)); // a
        mesh.Positions.Add(new Vec3(1, 0, 0)); // b
        mesh.Positions.Add(new Vec3(0, 1, 0)); // c
        mesh.Positions.Add(new Vec3(0, c, s)); // d

        mesh.Faces.Add(new[] { 0, 1, 2 });
        mesh.Faces.Add(new[] { 0, 1, 3 });
        mesh.RebuildDerived();
        return mesh;
    }
}
