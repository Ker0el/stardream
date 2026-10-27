using StarrySky.Core.Meshes;
using StarrySky.Core.Scene;
using Xunit;

namespace StarrySky.Core.Tests;

public class PrimitivesTests
{
    [Fact]
    public void Cube_Has_8_Vertices_6_Quads_12_Edges()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());

        Assert.Equal(8, mesh.Positions.Count);
        Assert.Equal(6, mesh.Faces.Count);
        Assert.All(mesh.Faces, f => Assert.Equal(4, f.Length));
        Assert.Equal(12, mesh.Edges.Count);
    }

    [Fact]
    public void Sphere_With_16_Segments_And_8_Rings_Has_128_Faces()
    {
        var mesh = Primitives.Build(PrimitiveParams.Sphere());

        Assert.Equal(128, mesh.Faces.Count);
    }

    [Fact]
    public void Cylinder_Closes_Both_Caps()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cylinder());

        var caps = mesh.Faces.Where(f => f.Length > 4).Count();
        Assert.Equal(2, caps);
    }

    [Fact]
    public void Plane_Is_One_Quad()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());

        Assert.Single(mesh.Faces);
        Assert.Equal(4, mesh.Faces[0].Length);
    }

    [Fact]
    public void Build_With_Zero_Segments_Clamps_To_Minimum()
    {
        var parameters = PrimitiveParams.Sphere();
        parameters.Segments = 0;
        parameters.Rings = 0;

        var mesh = Primitives.Build(parameters);

        Assert.True(mesh.Faces.Count > 0);
        Assert.True(mesh.Positions.Count > 0);
    }

    [Fact]
    public void BaseOffset_Lifts_Primitives_So_They_Sit_On_The_Ground()
    {
        Assert.Equal(0.5f, Primitives.BaseOffsetY(PrimitiveParams.Cube()), 3);
        Assert.Equal(0.5f, Primitives.BaseOffsetY(PrimitiveParams.Cylinder()), 3);
        Assert.Equal(0.5f, Primitives.BaseOffsetY(PrimitiveParams.Sphere()), 3);
        Assert.Equal(0f, Primitives.BaseOffsetY(PrimitiveParams.Plane()), 3);
    }
}
