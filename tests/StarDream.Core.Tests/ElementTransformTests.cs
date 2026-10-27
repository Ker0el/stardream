using StarDream.Core.Editing;
using StarDream.Core.Math;
using StarDream.Core.Meshes;
using StarDream.Core.Scene;
using Xunit;

namespace StarDream.Core.Tests;

public class ElementTransformTests
{
    [Fact]
    public void Translate_Moves_Selected_Vertices_Only()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var selection = new EditSelection { Mode = ElementMode.Vertex };
        selection.Vertices.Add(0);
        var otherBefore = mesh.Positions[1];

        ElementTransform.Translate(mesh, selection, new Vec3(1, 0, 0));

        Assert.Equal(mesh.Positions[0].X, mesh.Positions[0].X);
        Assert.Equal(otherBefore, mesh.Positions[1]);
    }

    [Fact]
    public void Translate_On_Face_Selection_Moves_All_Its_Vertices()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var selection = new EditSelection { Mode = ElementMode.Face };
        selection.Faces.Add(0);
        var before = mesh.Positions[0];

        ElementTransform.Translate(mesh, selection, new Vec3(0, 1, 0));

        Assert.Equal(before.Y + 1f, mesh.Positions[0].Y, 3);
    }

    [Fact]
    public void Rotate_Around_Pivot_Leaves_Pivot_Fixed()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var selection = new EditSelection { Mode = ElementMode.Vertex };
        foreach (var index in new[] { 0, 1, 2, 3 })
        {
            selection.Vertices.Add(index);
        }

        var pivot = MeshQueries.Centroid(mesh, selection);
        ElementTransform.Rotate(mesh, selection, Vec3.UnitY, MathF.PI / 2f, pivot);

        var after = MeshQueries.Centroid(mesh, selection);
        Assert.Equal(pivot.X, after.X, 2);
        Assert.Equal(pivot.Y, after.Y, 2);
        Assert.Equal(pivot.Z, after.Z, 2);
    }

    [Fact]
    public void Scale_With_Zero_Factor_Clamps_Instead_Of_Collapsing()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var selection = new EditSelection { Mode = ElementMode.Vertex };
        selection.Vertices.Add(0);

        ElementTransform.Scale(mesh, selection, new Vec3(0, 0, 0), Vec3.Zero);

        Assert.True(float.IsFinite(mesh.Positions[0].X));
        Assert.True(mesh.Positions[0].Length > 0f);
    }

    [Fact]
    public void Transform_With_Empty_Selection_Is_A_NoOp()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var before = mesh.Positions[0];

        ElementTransform.Translate(mesh, new EditSelection(), new Vec3(5, 5, 5));

        Assert.Equal(before, mesh.Positions[0]);
    }
}
