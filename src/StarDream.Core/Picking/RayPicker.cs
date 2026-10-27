using System;
using StarDream.Core.Math;
using StarDream.Core.Scene;

namespace StarDream.Core.Picking;

/// <summary>Ray-triangle intersection and scene level object picking.</summary>
public static class RayPicker
{
    private const float Epsilon = 1e-7f;

    public static bool RayTriangle(Ray ray, Vec3 a, Vec3 b, Vec3 c, out float distance)
    {
        distance = 0f;

        var edge1 = b - a;
        var edge2 = c - a;
        var h = Vec3.Cross(ray.Direction, edge2);
        var determinant = Vec3.Dot(edge1, h);

        if (MathF.Abs(determinant) < Epsilon)
        {
            return false;
        }

        var inverse = 1f / determinant;
        var s = ray.Origin - a;
        var u = Vec3.Dot(s, h) * inverse;

        if (u < 0f || u > 1f)
        {
            return false;
        }

        var q = Vec3.Cross(s, edge1);
        var v = Vec3.Dot(ray.Direction, q) * inverse;

        if (v < 0f || u + v > 1f)
        {
            return false;
        }

        var t = Vec3.Dot(edge2, q) * inverse;
        if (t <= Epsilon)
        {
            return false;
        }

        distance = t;
        return true;
    }

    /// <summary>Nearest visible object under the ray, or null.</summary>
    public static SceneObject? PickObject(SceneDocument document, Ray worldRay)
    {
        SceneObject? best = null;
        var bestDistance = float.MaxValue;

        foreach (var obj in document.Objects)
        {
            if (!obj.Visible)
            {
                continue;
            }

            if (!obj.LocalMatrix().TryInvert(out var inverse))
            {
                continue;
            }

            var localRay = new Ray(
                inverse.TransformPoint(worldRay.Origin),
                inverse.TransformDirection(worldRay.Direction));

            var mesh = obj.GetMesh();
            foreach (var triangle in mesh.Triangles)
            {
                if (!RayTriangle(
                        localRay,
                        mesh.Positions[triangle[0]],
                        mesh.Positions[triangle[1]],
                        mesh.Positions[triangle[2]],
                        out var distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = obj;
                }
            }
        }

        return best;
    }
}
