using System;
using System.Collections.Generic;
using StarrySky.Core.Math;
using StarrySky.Core.Meshes;

namespace StarrySky.Core.Picking;

/// <summary>Screen-space picking of individual mesh elements.</summary>
public static class ElementPicker
{
    public static int? PickVertex(
        Mesh mesh,
        Mat4 model,
        Mat4 viewProjection,
        Vec2 screenPixels,
        int viewportWidth,
        int viewportHeight,
        float radiusPixels = 8f)
    {
        var transform = viewProjection * model;
        int? best = null;
        var bestScore = radiusPixels;
        var bestDepth = float.MaxValue;

        for (var i = 0; i < mesh.Positions.Count; i++)
        {
            if (!Project(mesh.Positions[i], transform, viewportWidth, viewportHeight, out var screen, out var depth))
            {
                continue;
            }

            var distance = (screen - screenPixels).Length;
            if (distance <= bestScore && (distance < bestScore || depth < bestDepth))
            {
                best = i;
                bestScore = distance;
                bestDepth = depth;
            }
        }

        return best;
    }

    public static Edge? PickEdge(
        Mesh mesh,
        Mat4 model,
        Mat4 viewProjection,
        Vec2 screenPixels,
        int viewportWidth,
        int viewportHeight,
        float radiusPixels = 6f)
    {
        var transform = viewProjection * model;
        Edge? best = null;
        var bestDistance = radiusPixels;

        foreach (var edge in mesh.Edges)
        {
            if (!Project(mesh.Positions[edge.A], transform, viewportWidth, viewportHeight, out var a, out _)
                || !Project(mesh.Positions[edge.B], transform, viewportWidth, viewportHeight, out var b, out _))
            {
                continue;
            }

            var distance = DistanceToSegment(screenPixels, a, b);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = edge;
            }
        }

        return best;
    }

    public static int? PickFace(Mesh mesh, Mat4 model, Ray worldRay)
    {
        if (!model.TryInvert(out var inverse))
        {
            return null;
        }

        var localRay = new Ray(
            inverse.TransformPoint(worldRay.Origin),
            inverse.TransformDirection(worldRay.Direction));

        int? best = null;
        var bestDepth = float.MaxValue;

        for (var f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            for (var i = 1; i < face.Length - 1; i++)
            {
                if (!RayPicker.RayTriangle(
                        localRay,
                        mesh.Positions[face[0]],
                        mesh.Positions[face[i]],
                        mesh.Positions[face[i + 1]],
                        out var distance))
                {
                    continue;
                }

                if (distance < bestDepth)
                {
                    bestDepth = distance;
                    best = f;
                }
            }
        }

        return best;
    }

    private static bool Project(Vec3 world, Mat4 transform, int width, int height, out Vec2 screen, out float depth)
    {
        var clip = transform.TransformPoint(world);

        if (clip.Z < 0f || clip.Z > 1f)
        {
            screen = Vec2.Zero;
            depth = 0f;
            return false;
        }

        screen = new Vec2(
            (clip.X * 0.5f + 0.5f) * width,
            (1f - (clip.Y * 0.5f + 0.5f)) * height);
        depth = clip.Z;
        return true;
    }

    private static float DistanceToSegment(Vec2 point, Vec2 a, Vec2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared;

        if (lengthSquared < 1e-6f)
        {
            return (point - a).Length;
        }

        var t = Vec2.Dot(point - a, ab) / lengthSquared;
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        var closest = a + ab * t;
        return (point - closest).Length;
    }
}
