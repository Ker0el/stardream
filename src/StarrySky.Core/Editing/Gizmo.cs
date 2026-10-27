using System;
using StarrySky.Core.Math;
using StarrySky.Core.Scene;

namespace StarrySky.Core.Editing;

public enum GizmoMode
{
    Translate,
    Rotate,
    Scale,
}

public enum GizmoAxis
{
    X,
    Y,
    Z,
    Center,
}

public static class GizmoMath
{
    public static Vec3 AxisVector(GizmoAxis axis) => axis switch
    {
        GizmoAxis.X => Vec3.UnitX,
        GizmoAxis.Y => Vec3.UnitY,
        GizmoAxis.Z => Vec3.UnitZ,
        _ => Vec3.Zero,
    };

    /// <summary>Distance along <paramref name="axis"/> of the point on the axis closest to the ray.</summary>
    public static float? ParameterOnAxis(Ray ray, Vec3 origin, Vec3 axis, Vec3 viewDirection)
    {
        var planeNormal = Vec3.Cross(axis, Vec3.Cross(viewDirection, axis));
        if (planeNormal.LengthSquared < 1e-6f)
        {
            return null;
        }

        planeNormal = planeNormal.Normalized();
        var denominator = Vec3.Dot(ray.Direction, planeNormal);
        if (MathF.Abs(denominator) < 1e-6f)
        {
            return null;
        }

        var t = Vec3.Dot(origin - ray.Origin, planeNormal) / denominator;
        if (t <= 0f)
        {
            return null;
        }

        return Vec3.Dot(ray.At(t) - origin, axis);
    }

    /// <summary>Signed angle, in radians, of the ray hit around <paramref name="axis"/>.</summary>
    public static float? AngleOnAxis(Ray ray, Vec3 origin, Vec3 axis)
    {
        var normal = axis.Normalized();
        var denominator = Vec3.Dot(ray.Direction, normal);
        if (MathF.Abs(denominator) < 1e-6f)
        {
            return null;
        }

        var t = Vec3.Dot(origin - ray.Origin, normal) / denominator;
        if (t <= 0f)
        {
            return null;
        }

        var hit = ray.At(t) - origin;
        var reference = MathF.Abs(normal.Y) < 0.9f ? Vec3.UnitY : Vec3.UnitX;
        var tangent = Vec3.Cross(normal, reference).Normalized();
        var bitangent = Vec3.Cross(normal, tangent);

        return MathF.Atan2(Vec3.Dot(hit, bitangent), Vec3.Dot(hit, tangent));
    }
}

/// <summary>An in-progress gizmo drag. Apply <see cref="Update"/> as the pointer moves.</summary>
public sealed class GizmoDrag
{
    private readonly SceneObject _target;
    private readonly GizmoMode _mode;
    private readonly GizmoAxis _axis;
    private readonly Vec3 _origin;
    private readonly Vec3 _axisVector;
    private readonly float _startParameter;
    private readonly float _startAngle;
    private readonly Vec3 _startPosition;
    private readonly Vec3 _startRotation;
    private readonly Vec3 _startScale;
    private readonly float _scale;

    private GizmoDrag(
        SceneObject target,
        GizmoMode mode,
        GizmoAxis axis,
        Vec3 origin,
        Vec3 axisVector,
        float startParameter,
        float startAngle,
        float scale)
    {
        _target = target;
        _mode = mode;
        _axis = axis;
        _origin = origin;
        _axisVector = axisVector;
        _startParameter = startParameter;
        _startAngle = startAngle;
        _scale = scale;
        _startPosition = target.Position;
        _startRotation = target.Rotation;
        _startScale = target.Scale;
    }

    public static GizmoDrag? Begin(
        SceneObject target,
        GizmoMode mode,
        GizmoAxis axis,
        Ray ray,
        Vec3 viewDirection,
        float scale)
    {
        var origin = target.Position;
        var axisVector = axis == GizmoAxis.Center
            ? Vec3.UnitY
            : AxisFor(target, mode, axis);

        if (mode == GizmoMode.Rotate)
        {
            var angle = GizmoMath.AngleOnAxis(ray, origin, axisVector);
            return angle is null ? null : new GizmoDrag(target, mode, axis, origin, axisVector, 0f, angle.Value, scale);
        }

        var parameter = GizmoMath.ParameterOnAxis(ray, origin, axisVector, viewDirection);
        if (parameter is null)
        {
            return null;
        }

        var startPosition = target.Position;
        if (mode == GizmoMode.Scale && axis == GizmoAxis.Center)
        {
            // Uniform scale reads the vertical drag instead of an axis.
            var denominator = Vec3.Dot(ray.Direction, Vec3.UnitY);
            if (MathF.Abs(denominator) < 1e-6f)
            {
                return null;
            }

            var t = Vec3.Dot(origin - ray.Origin, Vec3.UnitY) / denominator;
            if (t <= 0f)
            {
                return null;
            }

            parameter = ray.At(t).Y;
        }

        return new GizmoDrag(target, mode, axis, startPosition, axisVector, parameter.Value, 0f, scale);
    }

    public void Update(Ray ray, Vec3 viewDirection)
    {
        switch (_mode)
        {
            case GizmoMode.Translate:
            {
                var parameter = GizmoMath.ParameterOnAxis(ray, _origin, _axisVector, viewDirection);
                if (parameter is null)
                {
                    return;
                }

                var delta = (parameter.Value - _startParameter) * _axisVector;
                _target.Position = _startPosition + delta;
                break;
            }

            case GizmoMode.Rotate:
            {
                var angle = GizmoMath.AngleOnAxis(ray, _origin, _axisVector);
                if (angle is null)
                {
                    return;
                }

                var delta = angle.Value - _startAngle;
                while (delta > MathF.PI)
                {
                    delta -= 2f * MathF.PI;
                }

                while (delta <= -MathF.PI)
                {
                    delta += 2f * MathF.PI;
                }

                var degrees = delta * MathUtil.RadToDeg;
                _target.Rotation = _axis switch
                {
                    GizmoAxis.X => new Vec3(_startRotation.X + degrees, _startRotation.Y, _startRotation.Z),
                    GizmoAxis.Y => new Vec3(_startRotation.X, _startRotation.Y + degrees, _startRotation.Z),
                    GizmoAxis.Z => new Vec3(_startRotation.X, _startRotation.Y, _startRotation.Z + degrees),
                    _ => _startRotation,
                };
                break;
            }

            case GizmoMode.Scale:
            {
                float parameter;
                if (_axis == GizmoAxis.Center)
                {
                    var denominator = Vec3.Dot(ray.Direction, Vec3.UnitY);
                    if (MathF.Abs(denominator) < 1e-6f)
                    {
                        return;
                    }

                    var t = Vec3.Dot(_origin - ray.Origin, Vec3.UnitY) / denominator;
                    if (t <= 0f)
                    {
                        return;
                    }

                    parameter = ray.At(t).Y;
                }
                else
                {
                    var value = GizmoMath.ParameterOnAxis(ray, _origin, _axisVector, viewDirection);
                    if (value is null)
                    {
                        return;
                    }

                    parameter = value.Value;
                }

                var factor = MathF.Max(0.01f, 1f + (parameter - _startParameter) / MathF.Max(0.001f, _scale));
                _target.Scale = _axis switch
                {
                    GizmoAxis.X => new Vec3(_startScale.X * factor, _startScale.Y, _startScale.Z),
                    GizmoAxis.Y => new Vec3(_startScale.X, _startScale.Y * factor, _startScale.Z),
                    GizmoAxis.Z => new Vec3(_startScale.X, _startScale.Y, _startScale.Z * factor),
                    _ => _startScale * factor,
                };
                break;
            }
        }
    }

    private static Vec3 AxisFor(SceneObject target, GizmoMode mode, GizmoAxis axis)
    {
        var local = GizmoMath.AxisVector(axis);

        // Scale acts on the object's own axes; move and rotate stay world aligned.
        if (mode == GizmoMode.Scale)
        {
            return target.LocalMatrix().TransformDirection(local).Normalized();
        }

        return local;
    }
}
