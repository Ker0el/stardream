using System;
using System.IO;
using StarrySky.Core.Export;
using StarrySky.Core.Scene;

namespace StarrySky.Tools;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "export-demo")
        {
            Console.Error.WriteLine("usage: StarrySky.Tools export-demo <output.fbx>");
            return 2;
        }

        var path = Path.GetFullPath(args[1]);
        var document = DemoScene.Create();

        var ok = FbxExporter.Export(document, path);
        if (!ok)
        {
            Console.Error.WriteLine("export failed");
            return 1;
        }

        var info = new FileInfo(path);
        Console.WriteLine($"exported {document.Objects.Count} objects, {document.TriangleCount} triangles");
        Console.WriteLine($"{path} ({info.Length} bytes)");
        return 0;
    }
}
