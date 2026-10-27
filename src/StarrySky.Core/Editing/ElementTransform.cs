using System;
using StarrySky.Core.Math;
using StarrySky.Core.Meshes;

namespace StarrySky.Core.Editing;

/// <summary>Moves, rotates and scales selected mesh elements.</summary>
public static class ElementTransform
{
    public static void Translate(Mesh mesh, EditSelection selection, Vec3 delta)
    {
        MoveVertices(mesh, selection, index => mesh.Positions[index] + delta);
    }

    public static void Rotate(Mesh mesh, EditSelection selection, Vec3 axis, float angleRadians, Vec3 pivot)
    {
        var direction = axis.Normalized();
        if (direction.LengthSquared < 0.5f)
        {
            return;
        }

        var c = MathF.Cos(angleRadians);
        var s = MathF.Sin(angleRadians);

        MoveVertices(mesh, selection, index =>
        {
            var relative = mesh.Positions[index] - pivot;
            var rotated = relative * c
                + Vec3.Cross(direction, relative) * s
                + direction * (Vec3.Dot(direction, relative) * (1f - c));
            return pivot + rotated;
        });
    }

    public static void Scale(Mesh mesh, EditSelection selection, Vec3 factor, Vec3 pivot)
    {
        var safe = new Vec3(
            MathF.Abs(factor.X) < 0.01f ? 0.01f : factor.X,
            MathF.Abs(factor.Y) < 0.01f ? 0.01f : factor.Y,
            MathF.Abs(factor.Z) < 0.01f ? 0.01f : factor.Z);

        MoveVertices(mesh, selection, index =>
        {
            var relative = mesh.Positions[index] - pivot;
            return pivot + new Vec3(relative.X * safe.X, relative.Y * safe.Y, relative.Z * safe.Z);
        });
    }

    private static void MoveVertices(Mesh mesh, EditSelection selection, Func<int, Vec3> transform)
    {
        var vertices = selection.ResolveVertices(mesh);
        if (vertices.Count == 0)
        {
            return;
        }

        foreach (var index in vertices)
        {
            if (index >= 0 && index < mesh.Positions.Count)
            {
                mesh.Positions[index] = transform(index);
            }
        }

        mesh.RebuildDerived();
        Meshes.NormalSmoother.Compute(mesh);
    }
}
