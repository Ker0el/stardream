using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using StarrySky.Core.Meshes;
using StarrySky.Core.Scene;

namespace StarrySky.Core.Export;

/// <summary>
/// Writes the scene as a binary FBX aimed at Unreal Engine: Y-up, centimetres,
/// one node per object with its own pivot, triangulated geometry.
/// </summary>
public static class FbxExporter
{
    public static bool Export(SceneDocument document, string path)
    {
        var scene = FbxNative.ufbxw_create_scene(IntPtr.Zero);
        if (scene == IntPtr.Zero)
        {
            throw new InvalidOperationException("ufbx-write could not create a scene.");
        }

        var pinned = new List<GCHandle>();

        try
        {
            FbxNative.ufbxw_scene_set_coordinate_axes(scene, new FbxNative.CoordinateAxes
            {
                Right = FbxNative.AxisPositiveX,
                Up = FbxNative.AxisPositiveY,
                Front = FbxNative.AxisNegativeZ,
            });

            // Scene units are metres; FBX stores centimetres.
            FbxNative.ufbxw_scene_set_unit_scale_factor(scene, 100.0);

            foreach (var obj in document.Objects)
            {
                if (!obj.Visible)
                {
                    continue;
                }

                var mesh = obj.GetMesh();
                if (mesh.Triangles.Count == 0)
                {
                    continue;
                }

                AppendNode(scene, obj, mesh, pinned);
            }

            FbxNative.ufbxw_prepare_scene(scene, IntPtr.Zero);

            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return FbxNative.ufbxw_save_file(scene, path, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            foreach (var handle in pinned)
            {
                handle.Free();
            }

            FbxNative.ufbxw_free_scene(scene);
        }
    }

    private static void AppendNode(IntPtr scene, SceneObject obj, Mesh mesh, List<GCHandle> pinned)
    {
        var node = FbxNative.ufbxw_create_node(scene);
        FbxNative.ufbxw_set_name(scene, node, obj.Name);
        FbxNative.ufbxw_node_set_translation(scene, node, new FbxNative.Vec3
        {
            X = obj.Position.X,
            Y = obj.Position.Y,
            Z = obj.Position.Z,
        });        var vertices = new FbxNative.Vec3[mesh.Positions.Count];
        for (var i = 0; i < mesh.Positions.Count; i++)
        {
            var position = mesh.Positions[i];
            vertices[i] = new FbxNative.Vec3 { X = position.X, Y = position.Y, Z = position.Z };
        }

        var indices = new int[mesh.Triangles.Count * 3];
        var write = 0;
        foreach (var triangle in mesh.Triangles)
        {
            indices[write++] = triangle[0];
            indices[write++] = triangle[1];
            indices[write++] = triangle[2];
        }

        var vertexHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        var indexHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
        pinned.Add(vertexHandle);
        pinned.Add(indexHandle);

        var vertexBuffer = FbxNative.ufbxw_view_vec3_array(
            scene, vertexHandle.AddrOfPinnedObject(), (UIntPtr)vertices.Length);
        var indexBuffer = FbxNative.ufbxw_view_int_array(
            scene, indexHandle.AddrOfPinnedObject(), (UIntPtr)indices.Length);

        var fbxMesh = FbxNative.ufbxw_create_mesh(scene);
        FbxNative.ufbxw_mesh_set_vertices(scene, fbxMesh, vertexBuffer);
        FbxNative.ufbxw_mesh_set_triangles(scene, fbxMesh, indexBuffer);
        FbxNative.ufbxw_mesh_add_instance(scene, fbxMesh, node);
    }
}
