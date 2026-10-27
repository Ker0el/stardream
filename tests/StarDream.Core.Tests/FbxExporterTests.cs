using System;
using System.IO;
using StarDream.Core.Export;
using StarDream.Core.Scene;
using Xunit;

namespace StarDream.Core.Tests;

public class FbxExporterTests
{
    [Fact]
    public void Export_Single_Cube_Writes_A_Binary_Fbx_File()
    {
        var document = new SceneDocument();
        document.AddPrimitive(PrimitiveParams.Cube());
        var path = Path.Combine(Path.GetTempPath(), $"stardream-{Guid.NewGuid():N}.fbx");

        try
        {
            var ok = FbxExporter.Export(document, path);

            Assert.True(ok);
            Assert.True(File.Exists(path));

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 1024, $"file is only {bytes.Length} bytes");
            Assert.Equal("Kaydara FBX Binary  ", System.Text.Encoding.ASCII.GetString(bytes, 0, 20));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Export_Empty_Scene_Does_Not_Throw()
    {
        var document = new SceneDocument();
        var path = Path.Combine(Path.GetTempPath(), $"stardream-{Guid.NewGuid():N}.fbx");

        try
        {
            var ok = FbxExporter.Export(document, path);

            Assert.True(ok);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
