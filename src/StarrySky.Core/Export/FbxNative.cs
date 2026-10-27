using System;
using System.Runtime.InteropServices;

namespace StarrySky.Core.Export;

/// <summary>
/// P/Invoke surface for the vendored ufbx-write library (MIT / public domain).
/// Only the calls the exporter needs are declared; see native/StarrySky.FbxNative/ufbx_write.h
/// for the full API.
/// </summary>
internal static class FbxNative
{
    private const string Dll = "StarrySky.FbxNative";

    // ufbxw_real is double, so every ufbxw_vec3 is three doubles.
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vec3
    {
        public double X;
        public double Y;
        public double Z;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Vec2
    {
        public double X;
        public double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Id
    {
        public readonly ulong Value;

        public Id(ulong value) => Value = value;

        public bool IsNull => Value == 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CoordinateAxes
    {
        public int Right;
        public int Up;
        public int Front;
    }

    // ufbxw_coordinate_axis: +X=0, -X=1, +Y=2, -Y=3, +Z=4, -Z=5
    internal const int AxisPositiveX = 0;
    internal const int AxisPositiveY = 2;
    internal const int AxisNegativeZ = 5;

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern IntPtr ufbxw_create_scene(IntPtr opts);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_free_scene(IntPtr scene);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_scene_set_coordinate_axes(IntPtr scene, CoordinateAxes axes);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_scene_set_unit_scale_factor(IntPtr scene, double unitScale);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern Id ufbxw_create_node(IntPtr scene);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_set_name(IntPtr scene, Id id, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_node_set_translation(IntPtr scene, Id node, Vec3 translation);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern Id ufbxw_create_mesh(IntPtr scene);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_mesh_add_instance(IntPtr scene, Id mesh, Id node);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern Id ufbxw_view_vec3_array(IntPtr scene, IntPtr data, UIntPtr count);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern Id ufbxw_view_int_array(IntPtr scene, IntPtr data, UIntPtr count);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_mesh_set_vertices(IntPtr scene, Id mesh, Id vertices);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_mesh_set_triangles(IntPtr scene, Id mesh, Id indices);

    /// <summary>ufbxw_attribute_mapping::UFBXW_ATTRIBUTE_MAPPING_POLYGON_VERTEX.</summary>
    internal const int MappingPolygonVertex = 1;

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_mesh_set_normals(IntPtr scene, Id mesh, Id normals, int mapping);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_mesh_set_uvs(IntPtr scene, Id mesh, int set, Id uvs, int mapping);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern Id ufbxw_view_vec2_array(IntPtr scene, IntPtr data, UIntPtr count);

    // ufbxw_material_type: LAMBERT=0, PHONG=1, CUSTOM=2
    internal const int MaterialPhong = 1;

    // ufbxw_prop_type::UFBXW_PROP_TYPE_COLOR
    internal const int PropTypeColor = 14;

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern Id ufbxw_create_material(IntPtr scene, int type);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_add_vec3(
        IntPtr scene, Id id, [MarshalAs(UnmanagedType.LPUTF8Str)] string prop, int type, Vec3 value);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_node_set_material(IntPtr scene, Id node, UIntPtr index, Id material);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_mesh_set_single_material(IntPtr scene, Id mesh, int materialIndex);

    /// <summary>ufbxw_mesh_attribute::UFBXW_MESH_ATTRIBUTE_SMOOTHING.</summary>
    internal const int MeshAttributeSmoothing = 6;

    /// <summary>ufbxw_attribute_mapping::UFBXW_ATTRIBUTE_MAPPING_POLYGON.</summary>
    internal const int MappingPolygon = 4;

    /// <summary>Mirrors ufbxw_mesh_attribute_desc; layout is asserted by offset order.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct AttributeDesc
    {
        public uint BeginZero;
        public int Mapping;
        public ulong Values;
        public ulong Indices;
        public IntPtr NameData;
        public UIntPtr NameLength;
        public ulong ValuesW;
        [MarshalAs(UnmanagedType.I1)] public bool GenerateIndices;
        public uint EndZero;
    }

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_mesh_set_attribute(
        IntPtr scene, Id mesh, int attribute, int set, ref AttributeDesc desc);

    [DllImport(Dll, ExactSpelling = true)]
    internal static extern void ufbxw_prepare_scene(IntPtr scene, IntPtr opts);

    [DllImport(Dll, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool ufbxw_save_file(IntPtr scene, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, IntPtr opts, IntPtr error);
}
