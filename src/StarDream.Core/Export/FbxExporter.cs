using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using StarDream.Core.Editing;
using StarDream.Core.Math;
using StarDream.Core.Meshes;
using StarDream.Core.Scene;

namespace StarDream.Core.Export;

/// <summary>
/// Writes the scene as a binary FBX aimed at Unreal Engine: Y-up, centimetres,
/// one node per object with its own pivot and material, triangulated geometry with
/// per-corner normals and a box-projected UV0.
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
        });

        var vertices = new FbxNative.Vec3[mesh.Positions.Count];
        for (var i = 0; i < mesh.Positions.Count; i++)
        {
            var position = mesh.Positions[i];
            vertices[i] = new FbxNative.Vec3 { X = position.X, Y = position.Y, Z = position.Z };
        }

        var corners = mesh.Triangles.Count * 3;
        var indices = new int[corners];
        var normals = new FbxNative.Vec3[corners];
        var uvs = new FbxNative.Vec2[corners];
        var smoothing = new int[mesh.Triangles.Count];

        var groups = ComputeSmoothingGroups(mesh);
        var write = 0;
        var triangle = 0;
        for (var f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            var offset = mesh.CornerOffset(f);
            var normal = mesh.FaceNormals[f];

            for (var i = 1; i < face.Length - 1; i++)
            {
                AppendCorner(mesh, face[0], offset + 0, normal, indices, normals, uvs, ref write);
                AppendCorner(mesh, face[i], offset + i, normal, indices, normals, uvs, ref write);
                AppendCorner(mesh, face[i + 1], offset + i + 1, normal, indices, normals, uvs, ref write);
                smoothing[triangle++] = groups[f];
            }
        }

        var vertexBuffer = ViewBuffer(scene, vertices, pinned, FbxNative.ufbxw_view_vec3_array);
        var indexBuffer = ViewBuffer(scene, indices, pinned, FbxNative.ufbxw_view_int_array);
        var normalBuffer = ViewBuffer(scene, normals, pinned, FbxNative.ufbxw_view_vec3_array);
        var uvBuffer = ViewBuffer(scene, uvs, pinned, FbxNative.ufbxw_view_vec2_array);

        var fbxMesh = FbxNative.ufbxw_create_mesh(scene);
        FbxNative.ufbxw_mesh_set_vertices(scene, fbxMesh, vertexBuffer);
        FbxNative.ufbxw_mesh_set_triangles(scene, fbxMesh, indexBuffer);
        FbxNative.ufbxw_mesh_set_normals(scene, fbxMesh, normalBuffer, FbxNative.MappingPolygonVertex);
        FbxNative.ufbxw_mesh_set_uvs(scene, fbxMesh, 0, uvBuffer, FbxNative.MappingPolygonVertex);
        FbxNative.ufbxw_mesh_set_single_material(scene, fbxMesh, 0);

        var smoothingBuffer = ViewBuffer(scene, smoothing, pinned, FbxNative.ufbxw_view_int_array);
        var attribute = new FbxNative.AttributeDesc
        {
            Mapping = FbxNative.MappingPolygon,
            Values = smoothingBuffer.Value,
        };
        FbxNative.ufbxw_mesh_set_attribute(
            scene, fbxMesh, FbxNative.MeshAttributeSmoothing, 0, ref attribute);

        FbxNative.ufbxw_mesh_add_instance(scene, fbxMesh, node);

        var color = obj.Color;
        var material = FbxNative.ufbxw_create_material(scene, FbxNative.MaterialPhong);
        FbxNative.ufbxw_set_name(scene, material, obj.Name + "_mat");
        FbxNative.ufbxw_add_vec3(scene, material, "DiffuseColor", FbxNative.PropTypeColor, new FbxNative.Vec3
        {
            X = color.X,
            Y = color.Y,
            Z = color.Z,
        });
        FbxNative.ufbxw_node_set_material(scene, node, UIntPtr.Zero, material);
    }

    /// <summary>
    /// Smoothing group per face, derived from the same 30° rule the viewport uses.
    /// Faces joined by an edge under the threshold share an id; ids start at 1
    /// because 0 means "no smoothing group" in FBX.
    /// </summary>
    private static int[] ComputeSmoothingGroups(Mesh mesh)
    {
        var parent = new int[mesh.Faces.Count];
        for (var i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        var threshold = MathF.Cos(NormalSmoother.SmoothAngleDegrees * MathUtil.DegToRad);

        foreach (var edge in mesh.Edges)
        {
            var faces = MeshQueries.FacesOfEdge(mesh, edge);
            for (var i = 0; i < faces.Count; i++)
            {
                for (var j = i + 1; j < faces.Count; j++)
                {
                    if (Vec3.Dot(mesh.FaceNormals[faces[i]], mesh.FaceNormals[faces[j]]) >= threshold)
                    {
                        Union(faces[i], faces[j]);
                    }
                }
            }
        }

        var ids = new Dictionary<int, int>();
        var result = new int[mesh.Faces.Count];
        for (var f = 0; f < result.Length; f++)
        {
            var root = Find(f);
            if (!ids.TryGetValue(root, out var id))
            {
                id = ids.Count + 1;
                ids[root] = id;
            }

            result[f] = id;
        }

        return result;

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        void Union(int a, int b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA != rootB)
            {
                parent[rootB] = rootA;
            }
        }
    }

    private delegate FbxNative.Id ViewBufferFunc(IntPtr scene, IntPtr data, UIntPtr count);

    private static FbxNative.Id ViewBuffer<T>(IntPtr scene, T[] data, List<GCHandle> pinned, ViewBufferFunc view)
        where T : struct
    {
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        pinned.Add(handle);
        return view(scene, handle.AddrOfPinnedObject(), (UIntPtr)data.Length);
    }

    private static void AppendCorner(
        Mesh mesh,
        int vertexIndex,
        int cornerIndex,
        Vec3 faceNormal,
        int[] indices,
        FbxNative.Vec3[] normals,
        FbxNative.Vec2[] uvs,
        ref int write)
    {
        var normal = cornerIndex < mesh.CornerNormals.Length ? mesh.CornerNormals[cornerIndex] : faceNormal;
        var position = mesh.Positions[vertexIndex];

        indices[write] = vertexIndex;
        normals[write] = new FbxNative.Vec3 { X = normal.X, Y = normal.Y, Z = normal.Z };

        var uv = BoxProject(position, faceNormal);
        uvs[write] = new FbxNative.Vec2 { X = uv.X, Y = uv.Y };

        write++;
    }

    /// <summary>Planar projection onto the plane the face normal points along.</summary>
    private static Vec2 BoxProject(Vec3 position, Vec3 normal)
    {
        var ax = MathF.Abs(normal.X);
        var ay = MathF.Abs(normal.Y);
        var az = MathF.Abs(normal.Z);

        if (ax >= ay && ax >= az)
        {
            return new Vec2(position.Z * 0.5f, position.Y * 0.5f);
        }

        if (ay >= ax && ay >= az)
        {
            return new Vec2(position.X * 0.5f, position.Z * 0.5f);
        }

        return new Vec2(position.X * 0.5f, position.Y * 0.5f);
    }
}
