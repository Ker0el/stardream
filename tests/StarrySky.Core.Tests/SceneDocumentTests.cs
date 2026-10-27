using StarrySky.Core.Math;
using StarrySky.Core.Scene;
using Xunit;

namespace StarrySky.Core.Tests;

public class SceneDocumentTests
{
    [Fact]
    public void AddPrimitive_Places_Bottom_On_Ground()
    {
        var document = new SceneDocument();

        var cube = document.AddPrimitive(PrimitiveParams.Cube());

        Assert.Equal(0.5f, cube.Position.Y, 3);
        Assert.Same(cube, document.Selected);
    }

    [Fact]
    public void Changing_Primitive_Params_And_Invalidating_Rebuilds_Mesh()
    {
        var document = new SceneDocument();
        var sphere = document.AddPrimitive(PrimitiveParams.Sphere());
        var before = sphere.GetMesh().Faces.Count;

        sphere.Primitive!.Segments = 8;
        sphere.Invalidate();

        Assert.True(sphere.GetMesh().Faces.Count < before);
    }

    [Fact]
    public void Bake_Freezes_Params_And_Mesh()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());

        var baked = cube.Bake();

        Assert.True(cube.IsBaked);
        Assert.Null(cube.Primitive);
        Assert.Same(baked, cube.GetMesh());
        Assert.Equal(6, baked.Faces.Count);
    }

    [Fact]
    public void Duplicate_Offsets_X_By_Half_And_Gets_New_Id()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());

        var copy = document.Duplicate(cube);

        Assert.NotEqual(cube.Id, copy.Id);
        Assert.Equal(cube.Position.X + 0.5f, copy.Position.X, 3);
        Assert.Equal(2, document.Objects.Count);
        Assert.Same(copy, document.Selected);
    }

    [Fact]
    public void Remove_Drops_Object_From_Document()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());

        document.Remove(cube);

        Assert.Empty(document.Objects);
        Assert.Null(document.Selected);
    }

    [Fact]
    public void Duplicate_Of_Baked_Mesh_Keeps_Baked_Geometry()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Bake();

        var copy = document.Duplicate(cube);

        Assert.True(copy.IsBaked);
        Assert.Equal(cube.GetMesh().Faces.Count, copy.GetMesh().Faces.Count);
    }

    [Fact]
    public void Add_Puts_An_Externally_Built_Object_Into_The_Document()
    {
        var document = new SceneDocument();
        var pasted = SceneObject.FromPrimitive(PrimitiveParams.Sphere(), "粘贴的球");

        document.Add(pasted);

        Assert.Single(document.Objects);
        Assert.Same(pasted, document.Selected);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void ColorHex_Parses_To_Linear_Components()
    {
        var obj = SceneObject.FromPrimitive(PrimitiveParams.Cube(), "c");
        obj.ColorHex = "#FF8000";

        var color = obj.Color;

        Assert.Equal(1f, color.X, 3);
        Assert.Equal(0.502f, color.Y, 2);
        Assert.Equal(0f, color.Z, 3);
    }

    [Fact]
    public void LocalMatrix_Applies_Transform()
    {
        var obj = SceneObject.FromPrimitive(PrimitiveParams.Cube(), "c");
        obj.Position = new Vec3(2, 0, 0);

        var p = obj.LocalMatrix().TransformPoint(Vec3.Zero);

        Assert.Equal(2f, p.X, 3);
    }
}
