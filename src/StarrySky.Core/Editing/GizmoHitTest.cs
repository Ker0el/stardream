using System;
using StarrySky.Core.Math;
using StarrySky.Core.Scene;

namespace StarrySky.Core.Editing;

/// <summary>Screen-space hit testing for the transform gizmo handles.</summary>
public static class GizmoHitTest
{
    private const int RingSamples = 32;

    public static GizmoAxis? Hit(
        SceneObject target,
        GizmoMode mode,
        Mat4 viewProjection,
        Vec2 screen,
        int viewportWidth,
        int viewportHeight,
        float scale,
        float thresholdPixels = 8f)
    {
        var origin = target.Position;

        if (!Project(origin, viewProjection, viewportWidth, viewportHeight, out var originScreen))
        {
            return null;
        }

        if ((screen - originScreen).Length <= thresholdPixels + 2f)
        {
            return GizmoAxis.Center;
        }

        GizmoAxis? best = null;
        var bestDistance = thresholdPixels;

        foreach (var axis in new[] { GizmoAxis.X, GizmoAxis.Y, GizmoAxis.Z })
        {
            var direction = AxisFor(target, mode, axis);

            if (mode == GizmoMode.Rotate)
            {
                var ring = DistanceToRing(origin, direction, scale, viewProjection, screen, viewportWidth, viewportHeight);
                if (ring < bestDistance)
                {
                    bestDistance = ring;
                    best = axis;
                }

                continue;
            }

            if (!Project(origin + direction * scale, viewProjection, viewportWidth, viewportHeight, out var endScreen))
            {
                continue;
            }

            var distance = DistanceToSegment(screen, originScreen, endScreen);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = axis;
            }
        }

        return best;
    }

    private static Vec3 AxisFor(SceneObject target, GizmoMode mode, GizmoAxis axis)
    {
        var local = GizmoMath.AxisVector(axis);
        return mode == GizmoMode.Scale
            ? target.LocalMatrix().TransformDirection(local).Normalized()
            : local;
    }

    private static float DistanceToRing(
        Vec3 origin,
        Vec3 axis,
        float radius,
        Mat4 viewProjection,
        Vec2 screen,
        int width,
        int height)
    {
        var normal = axis.Normalized();
        var reference = MathF.Abs(normal.Y) < 0.9f ? Vec3.UnitY : Vec3.UnitX;
        var tangent = Vec3.Cross(normal, reference).Normalized();
        var bitangent = Vec3.Cross(normal, tangent);

        var best = float.MaxValue;
        Vec2? previous = null;

        for (var i = 0; i <= RingSamples; i++)
        {
            var angle = 2f * MathF.PI * i / RingSamples;
            var point = origin + (tangent * MathF.Cos(angle) + bitangent * MathF.Sin(angle)) * radius;

            if (!Project(point, viewProjection, width, height, out var projected))
            {
                previous = null;
                continue;
            }

            if (previous is not null)
            {
                best = MathF.Min(best, DistanceToSegment(screen, previous.Value, projected));
            }

            previous = projected;
        }

        return best;
    }

    private static bool Project(Vec3 world, Mat4 viewProjection, int width, int height, out Vec2 screen)
    {
        var clip = viewProjection.TransformPoint(world);
        if (clip.Z < 0f || clip.Z > 1f)
        {
            screen = Vec2.Zero;
            return false;
        }

        screen = new Vec2(
            (clip.X * 0.5f + 0.5f) * width,
            (1f - (clip.Y * 0.5f + 0.5f)) * height);
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
        return (point - (a + ab * t)).Length;
    }
}
