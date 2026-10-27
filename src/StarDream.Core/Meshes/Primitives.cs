using System;
using System.Collections.Generic;
using StarDream.Core.Math;
using StarDream.Core.Scene;

namespace StarDream.Core.Meshes;

/// <summary>Builds procedural primitives. All faces wind counter-clockwise seen from outside.</summary>
public static class Primitives
{
    private const int MinSegments = 3;
    private const int MinRings = 2;

    public static Mesh Build(PrimitiveParams parameters) => parameters.Type switch
    {
        "sphere" => Sphere(parameters),
        "cylinder" => Cylinder(parameters),
        "plane" => Plane(parameters),
        _ => Cube(parameters),
    };

    /// <summary>How far to lift the primitive so its base sits on the ground plane.</summary>
    public static float BaseOffsetY(PrimitiveParams parameters) => parameters.Type switch
    {
        "sphere" => MathF.Max(parameters.Radius, 0.01f),
        "cylinder" => MathF.Max(parameters.Height, 0.01f) * 0.5f,
        "plane" => 0f,
        _ => MathF.Max(parameters.Size, 0.01f) * 0.5f,
    };

    private static Mesh Cube(PrimitiveParams parameters)
    {
        var h = MathF.Max(parameters.Size, 0.01f) * 0.5f;
        var mesh = new Mesh();

        mesh.Positions.AddRange(new[]
        {
            new Vec3(-h, -h, -h),
            new Vec3(h, -h, -h),
            new Vec3(h, h, -h),
            new Vec3(-h, h, -h),
            new Vec3(-h, -h, h),
            new Vec3(h, -h, h),
            new Vec3(h, h, h),
            new Vec3(-h, h, h),
        });

        mesh.Faces.Add(new[] { 4, 5, 6, 7 }); // +Z
        mesh.Faces.Add(new[] { 1, 0, 3, 2 }); // -Z
        mesh.Faces.Add(new[] { 5, 1, 2, 6 }); // +X
        mesh.Faces.Add(new[] { 0, 4, 7, 3 }); // -X
        mesh.Faces.Add(new[] { 7, 6, 2, 3 }); // +Y
        mesh.Faces.Add(new[] { 0, 1, 5, 4 }); // -Y

        mesh.RebuildDerived();
        return mesh;
    }

    private static Mesh Sphere(PrimitiveParams parameters)
    {
        var radius = MathF.Max(parameters.Radius, 0.01f);
        var segments = System.Math.Max(parameters.Segments, MinSegments);
        var rings = System.Math.Max(parameters.Rings, MinRings);

        var mesh = new Mesh();
        var north = 0;
        mesh.Positions.Add(new Vec3(0, radius, 0));

        // Rings 1..rings-1, from north to south.
        var ringStarts = new int[rings];
        for (var ring = 1; ring < rings; ring++)
        {
            var theta = MathF.PI * ring / rings;
            var y = radius * MathF.Cos(theta);
            var r = radius * MathF.Sin(theta);
            ringStarts[ring] = mesh.Positions.Count;

            for (var segment = 0; segment < segments; segment++)
            {
                var phi = 2f * MathF.PI * segment / segments;
                mesh.Positions.Add(new Vec3(r * MathF.Cos(phi), y, r * MathF.Sin(phi)));
            }
        }

        var south = mesh.Positions.Count;
        mesh.Positions.Add(new Vec3(0, -radius, 0));

        // North cap.
        var first = ringStarts[1];
        for (var segment = 0; segment < segments; segment++)
        {
            var j = segment;
            var k = (segment + 1) % segments;
            mesh.Faces.Add(new[] { first + j, north, first + k });
        }

        // Middle bands.
        for (var ring = 1; ring < rings - 1; ring++)
        {
            var upper = ringStarts[ring];
            var lower = ringStarts[ring + 1];
            for (var segment = 0; segment < segments; segment++)
            {
                var j = segment;
                var k = (segment + 1) % segments;
                mesh.Faces.Add(new[] { lower + j, upper + j, upper + k, lower + k });
            }
        }

        // South cap.
        var last = ringStarts[rings - 1];
        for (var segment = 0; segment < segments; segment++)
        {
            var j = segment;
            var k = (segment + 1) % segments;
            mesh.Faces.Add(new[] { south, last + j, last + k });
        }

        mesh.RebuildDerived();
        return mesh;
    }

    private static Mesh Cylinder(PrimitiveParams parameters)
    {
        var radius = MathF.Max(parameters.Radius, 0.01f);
        var height = MathF.Max(parameters.Height, 0.01f);
        var segments = System.Math.Max(parameters.Segments, MinSegments);
        var half = height * 0.5f;

        var mesh = new Mesh();
        var bottomStart = mesh.Positions.Count;
        for (var segment = 0; segment < segments; segment++)
        {
            var phi = 2f * MathF.PI * segment / segments;
            mesh.Positions.Add(new Vec3(radius * MathF.Cos(phi), -half, radius * MathF.Sin(phi)));
        }

        var topStart = mesh.Positions.Count;
        for (var segment = 0; segment < segments; segment++)
        {
            var phi = 2f * MathF.PI * segment / segments;
            mesh.Positions.Add(new Vec3(radius * MathF.Cos(phi), half, radius * MathF.Sin(phi)));
        }

        for (var segment = 0; segment < segments; segment++)
        {
            var j = segment;
            var k = (segment + 1) % segments;
            mesh.Faces.Add(new[] { bottomStart + j, topStart + j, topStart + k, bottomStart + k });
        }

        var bottomCap = new int[segments];
        var topCap = new int[segments];
        for (var segment = 0; segment < segments; segment++)
        {
            bottomCap[segment] = bottomStart + segment;
            topCap[segment] = topStart + (segments - 1 - segment);
        }

        mesh.Faces.Add(bottomCap);
        mesh.Faces.Add(topCap);

        mesh.RebuildDerived();
        return mesh;
    }

    private static Mesh Plane(PrimitiveParams parameters)
    {
        var halfWidth = MathF.Max(parameters.Width, 0.01f) * 0.5f;
        var halfDepth = MathF.Max(parameters.Depth, 0.01f) * 0.5f;

        var mesh = new Mesh();
        mesh.Positions.AddRange(new[]
        {
            new Vec3(-halfWidth, 0, halfDepth),
            new Vec3(halfWidth, 0, halfDepth),
            new Vec3(halfWidth, 0, -halfDepth),
            new Vec3(-halfWidth, 0, -halfDepth),
        });

        mesh.Faces.Add(new[] { 0, 1, 2, 3 });

        mesh.RebuildDerived();
        return mesh;
    }
}
