using StarDream.Core.Meshes;
using StarDream.Core.Math;
using Xunit;

namespace StarDream.Core.Tests;

public class MeshTests
{
    [Fact]
    public void Rebuild_Dedupes_Shared_Edges()
    {
        var mesh = new Mesh();
        mesh.Positions.AddRange(new[]
        {
            new Vec3(0, 0, 0),
            new Vec3(1, 0, 0),
            new Vec3(1, 1, 0),
            new Vec3(0, 1, 0),
        });
        mesh.Faces.Add(new[] { 0, 1, 2 });
        mesh.Faces.Add(new[] { 0, 2, 3 });

        mesh.RebuildDerived();

        Assert.Equal(5, mesh.Edges.Count);
    }

    [Fact]
    public void Triangulation_Fan_Splits_Quad_Into_Two_Triangles()
    {
        var mesh = new Mesh();
        mesh.Positions.AddRange(new[]
        {
            new Vec3(0, 0, 0),
            new Vec3(1, 0, 0),
            new Vec3(1, 0, 1),
            new Vec3(0, 0, 1),
        });
        mesh.Faces.Add(new[] { 0, 1, 2, 3 });

        mesh.RebuildDerived();

        Assert.Equal(2, mesh.Triangles.Count);
        Assert.Equal(new[] { 0, 1, 2 }, mesh.Triangles[0]);
        Assert.Equal(new[] { 0, 2, 3 }, mesh.Triangles[1]);
    }

    [Fact]
    public void CornerOffset_Sums_Preceding_Face_Vertex_Counts()
    {
        var mesh = new Mesh();
        mesh.Positions.AddRange(new[]
        {
            new Vec3(0, 0, 0),
            new Vec3(1, 0, 0),
            new Vec3(1, 1, 0),
            new Vec3(0, 1, 0),
            new Vec3(0, 0, 1),
        });
        mesh.Faces.Add(new[] { 0, 1, 2 });
        mesh.Faces.Add(new[] { 0, 1, 2, 3 });
        mesh.Faces.Add(new[] { 1, 3, 4 });

        Assert.Equal(0, mesh.CornerOffset(0));
        Assert.Equal(3, mesh.CornerOffset(1));
        Assert.Equal(7, mesh.CornerOffset(2));
        Assert.Equal(10, mesh.CornerCount);
    }

    [Fact]
    public void Rebuild_On_Empty_Mesh_Does_Not_Throw()
    {
        var mesh = new Mesh();

        mesh.RebuildDerived();

        Assert.Empty(mesh.Edges);
        Assert.Empty(mesh.Triangles);
        Assert.Empty(mesh.FaceNormals);
        Assert.Empty(mesh.CornerNormals);
    }

    [Fact]
    public void Face_With_Out_Of_Range_Index_Is_Rejected()
    {
        var mesh = new Mesh();
        mesh.Positions.Add(new Vec3(0, 0, 0));
        mesh.Faces.Add(new[] { 0, 5, 9 });

        Assert.Throws<InvalidOperationException>(() => mesh.RebuildDerived());
    }
}
