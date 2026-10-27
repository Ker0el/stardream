using StarrySky.Core.Math;
using StarrySky.Core.Scene;
using Xunit;

namespace StarrySky.Core.Tests;

public class SceneSerializerTests
{
    [Fact]
    public void RoundTrip_Preserves_Objects_Transforms_And_Camera()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Position = new Vec3(1, 2, 3);
        cube.Rotation = new Vec3(10, 20, 30);
        cube.Scale = new Vec3(2, 2, 2);
        cube.ColorHex = "#123456";
        cube.Name = "方块";
        document.Camera.Distance = 7.5f;
        document.Camera.Yaw = 1.1f;

        var json = SceneSerializer.Save(document);
        var loaded = SceneSerializer.Load(json);

        Assert.Single(loaded.Objects);
        var restored = loaded.Objects[0];
        Assert.Equal("方块", restored.Name);
        Assert.Equal(cube.Position, restored.Position);
        Assert.Equal(cube.Rotation, restored.Rotation);
        Assert.Equal(cube.Scale, restored.Scale);
        Assert.Equal("#123456", restored.ColorHex);
        Assert.Equal(7.5f, loaded.Camera.Distance, 3);
        Assert.Equal(1.1f, loaded.Camera.Yaw, 3);
        Assert.False(loaded.IsDirty);
    }

    [Fact]
    public void Baked_Mesh_RoundTrips_With_Positions_And_Faces()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Bake();

        var json = SceneSerializer.Save(document);
        var loaded = SceneSerializer.Load(json);

        var restored = loaded.Objects[0];
        Assert.True(restored.IsBaked);
        Assert.Null(restored.Primitive);
        Assert.Equal(8, restored.GetMesh().Positions.Count);
        Assert.Equal(6, restored.GetMesh().Faces.Count);
    }

    [Fact]
    public void Unknown_Version_Throws()
    {
        var json = "{\"version\": 99, \"objects\": []}";

        Assert.Throws<SceneLoadException>(() => SceneSerializer.Load(json));
    }

    [Fact]
    public void Malformed_Json_Throws_And_Leaves_Document_Untouched()
    {
        var document = new SceneDocument();
        var cube = document.AddPrimitive(PrimitiveParams.Cube());
        cube.Name = "保留我";

        Assert.Throws<SceneLoadException>(() => SceneSerializer.LoadInto(document, "{ not json"));

        Assert.Single(document.Objects);
        Assert.Equal("保留我", document.Objects[0].Name);
    }

    [Fact]
    public void Empty_Scene_RoundTrips()
    {
        var document = new SceneDocument();

        var loaded = SceneSerializer.Load(SceneSerializer.Save(document));

        Assert.Empty(loaded.Objects);
    }
}
