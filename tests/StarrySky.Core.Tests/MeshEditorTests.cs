using System.Linq;
using StarrySky.Core.Editing;
using StarrySky.Core.Math;
using StarrySky.Core.Meshes;
using StarrySky.Core.Scene;
using Xunit;

namespace StarrySky.Core.Tests;

public class MeshEditorTests
{
    [Fact]
    public void BoundaryEdges_Of_Single_Quad_Is_Its_Four_Edges()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());

        var boundary = MeshQueries.BoundaryEdges(mesh, new HashSet<int> { 0 });

        Assert.Equal(4, boundary.Count);
    }

    [Fact]
    public void BoundaryEdges_Of_Whole_Cube_Is_Empty()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var all = Enumerable.Range(0, mesh.Faces.Count).ToHashSet();

        var boundary = MeshQueries.BoundaryEdges(mesh, all);

        Assert.Empty(boundary);
    }

    [Fact]
    public void ConnectedComponents_Splits_Two_Opposite_Faces()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var selection = new HashSet<int> { 0, 1 };

        var components = MeshQueries.ConnectedComponents(mesh, selection);

        Assert.Equal(2, components.Count);
    }

    [Fact]
    public void Centroid_Of_Empty_Selection_Is_Zero()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());

        Assert.Equal(Vec3.Zero, MeshQueries.Centroid(mesh, new EditSelection()));
    }

    [Fact]
    public void Delete_Face_Removes_Only_That_Face()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var selection = new EditSelection { Mode = ElementMode.Face };
        selection.Faces.Add(0);

        MeshEditor.Delete(mesh, selection);

        Assert.Equal(5, mesh.Faces.Count);
    }

    [Fact]
    public void Delete_All_Faces_Leaves_Empty_Mesh()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var selection = new EditSelection { Mode = ElementMode.Face };
        selection.Faces.UnionWith(Enumerable.Range(0, mesh.Faces.Count));

        MeshEditor.Delete(mesh, selection);

        Assert.Empty(mesh.Faces);
        Assert.Empty(mesh.Positions);
    }

    [Fact]
    public void Delete_With_Empty_Selection_Is_A_NoOp()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var before = mesh.Faces.Count;

        MeshEditor.Delete(mesh, new EditSelection());

        Assert.Equal(before, mesh.Faces.Count);
    }

    [Fact]
    public void Extrude_Single_Quad_Adds_Side_Faces_And_Moves_Cap()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var selection = new EditSelection { Mode = ElementMode.Face };
        selection.Faces.Add(0);
        var originalNormal = mesh.FaceNormals[0];

        var result = MeshEditor.Extrude(mesh, selection, 0.5f);

        Assert.True(result.Succeeded);
        Assert.Equal(5, mesh.Faces.Count);
        Assert.Equal(8, mesh.Positions.Count);

        var moved = mesh.Positions[mesh.Faces[0][0]];
        Assert.True(Vec3.Dot(moved, originalNormal) > 0.4f);
        Assert.Single(selection.Faces);
        Assert.Equal(0, selection.Faces.First());
    }

    [Fact]
    public void Extrude_Whole_Cube_Is_Rejected()
    {
        var mesh = Primitives.Build(PrimitiveParams.Cube());
        var selection = new EditSelection { Mode = ElementMode.Face };
        selection.Faces.UnionWith(Enumerable.Range(0, mesh.Faces.Count));

        var result = MeshEditor.Extrude(mesh, selection, 0.2f);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(6, mesh.Faces.Count);
    }

    [Fact]
    public void Extrude_With_Zero_Distance_Does_Not_Throw()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var selection = new EditSelection { Mode = ElementMode.Face };
        selection.Faces.Add(0);

        var result = MeshEditor.Extrude(mesh, selection, 0f);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Extrude_With_Empty_Selection_Returns_Error()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());

        var result = MeshEditor.Extrude(mesh, new EditSelection { Mode = ElementMode.Face }, 0.2f);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Extrude_In_Vertex_Mode_Is_Rejected()
    {
        var mesh = Primitives.Build(PrimitiveParams.Plane());
        var selection = new EditSelection { Mode = ElementMode.Vertex };
        selection.Vertices.Add(0);

        var result = MeshEditor.Extrude(mesh, selection, 0.2f);

        Assert.False(result.Succeeded);
    }
}
