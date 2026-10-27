using System;

namespace StarrySky.Core.Math;

/// <summary>
/// Row major 4x4 transform matrix used with column vectors: v' = M * v.
/// Row index first: M11 is row 1 column 1.
/// </summary>
public readonly struct Mat4
{
    public readonly float M11, M12, M13, M14;
    public readonly float M21, M22, M23, M24;
    public readonly float M31, M32, M33, M34;
    public readonly float M41, M42, M43, M44;

    public Mat4(
        float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    public static Mat4 Identity => new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);

    public static Mat4 operator *(Mat4 a, Mat4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
        a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
        a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
        a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
        a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
        a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
        a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
        a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
        a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
        a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
        a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
        a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
        a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44);

    /// <summary>Transforms a point and divides by W when the matrix is projective.</summary>
    public Vec3 TransformPoint(Vec3 p)
    {
        var x = M11 * p.X + M12 * p.Y + M13 * p.Z + M14;
        var y = M21 * p.X + M22 * p.Y + M23 * p.Z + M24;
        var z = M31 * p.X + M32 * p.Y + M33 * p.Z + M34;
        var w = M41 * p.X + M42 * p.Y + M43 * p.Z + M44;

        if (MathF.Abs(w) > 1e-8f && MathF.Abs(w - 1f) > 1e-8f)
        {
            return new Vec3(x / w, y / w, z / w);
        }

        return new Vec3(x, y, z);
    }

    /// <summary>Transforms a direction, ignoring translation.</summary>
    public Vec3 TransformDirection(Vec3 d) => new(
        M11 * d.X + M12 * d.Y + M13 * d.Z,
        M21 * d.X + M22 * d.Y + M23 * d.Z,
        M31 * d.X + M32 * d.Y + M33 * d.Z);

    public static Mat4 Translation(Vec3 t) => new(
        1, 0, 0, t.X,
        0, 1, 0, t.Y,
        0, 0, 1, t.Z,
        0, 0, 0, 1);

    public static Mat4 Scale(Vec3 s) => new(
        s.X, 0, 0, 0,
        0, s.Y, 0, 0,
        0, 0, s.Z, 0,
        0, 0, 0, 1);

    public static Mat4 RotationX(float radians)
    {
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);
        return new Mat4(
            1, 0, 0, 0,
            0, c, -s, 0,
            0, s, c, 0,
            0, 0, 0, 1);
    }

    public static Mat4 RotationY(float radians)
    {
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);
        return new Mat4(
            c, 0, s, 0,
            0, 1, 0, 0,
            -s, 0, c, 0,
            0, 0, 0, 1);
    }

    public static Mat4 RotationZ(float radians)
    {
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);
        return new Mat4(
            c, -s, 0, 0,
            s, c, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
    }

    /// <summary>Rotation from XYZ extrinsic Euler angles, composed R = Rz * Ry * Rx.</summary>
    public static Mat4 RotationFromEuler(Vec3 degrees) => RotationZ(degrees.Z * MathUtil.DegToRad)
        * RotationY(degrees.Y * MathUtil.DegToRad)
        * RotationX(degrees.X * MathUtil.DegToRad);

    /// <summary>Object transform: T * Rz * Ry * Rx * S.</summary>
    public static Mat4 FromTrs(Vec3 translation, Vec3 rotationDegrees, Vec3 scale)
    {
        if (!translation.IsFinite || !rotationDegrees.IsFinite || !scale.IsFinite)
        {
            throw new ArgumentException("TRS components must be finite numbers.");
        }

        return Translation(translation) * RotationFromEuler(rotationDegrees) * Scale(scale);
    }

    /// <summary>Right handed view matrix.</summary>
    public static Mat4 LookAt(Vec3 eye, Vec3 target, Vec3 up)
    {
        var zaxis = (eye - target).Normalized();
        if (zaxis.LengthSquared < 1e-8f)
        {
            zaxis = Vec3.UnitZ;
        }

        var xaxis = Vec3.Cross(up, zaxis).Normalized();
        if (xaxis.LengthSquared < 1e-8f)
        {
            xaxis = Vec3.Cross(Vec3.UnitZ, zaxis).Normalized();
        }

        var yaxis = Vec3.Cross(zaxis, xaxis);

        return new Mat4(
            xaxis.X, xaxis.Y, xaxis.Z, -Vec3.Dot(xaxis, eye),
            yaxis.X, yaxis.Y, yaxis.Z, -Vec3.Dot(yaxis, eye),
            zaxis.X, zaxis.Y, zaxis.Z, -Vec3.Dot(zaxis, eye),
            0, 0, 0, 1);
    }

    /// <summary>Right handed perspective projection mapping near to 0 and far to 1 (Direct3D).</summary>
    public static Mat4 PerspectiveFov(float fovYRadians, float aspect, float nearPlane, float farPlane)
    {
        var f = 1f / MathF.Tan(fovYRadians * 0.5f);
        var range = nearPlane - farPlane;

        return new Mat4(
            f / aspect, 0, 0, 0,
            0, f, 0, 0,
            0, 0, farPlane / range, nearPlane * farPlane / range,
            0, 0, -1, 0);
    }

    public bool TryInvert(out Mat4 result)
    {
        var m = new double[4, 8];
        m[0, 0] = M11; m[0, 1] = M12; m[0, 2] = M13; m[0, 3] = M14;
        m[1, 0] = M21; m[1, 1] = M22; m[1, 2] = M23; m[1, 3] = M24;
        m[2, 0] = M31; m[2, 1] = M32; m[2, 2] = M33; m[2, 3] = M34;
        m[3, 0] = M41; m[3, 1] = M42; m[3, 2] = M43; m[3, 3] = M44;
        m[0, 4] = 1; m[1, 5] = 1; m[2, 6] = 1; m[3, 7] = 1;

        for (var col = 0; col < 4; col++)
        {
            var pivot = col;
            for (var row = col + 1; row < 4; row++)
            {
                if (System.Math.Abs(m[row, col]) > System.Math.Abs(m[pivot, col]))
                {
                    pivot = row;
                }
            }

            if (System.Math.Abs(m[pivot, col]) < 1e-12)
            {
                result = Identity;
                return false;
            }

            if (pivot != col)
            {
                for (var k = 0; k < 8; k++)
                {
                    (m[col, k], m[pivot, k]) = (m[pivot, k], m[col, k]);
                }
            }

            var scale = m[col, col];
            for (var k = 0; k < 8; k++)
            {
                m[col, k] /= scale;
            }

            for (var row = 0; row < 4; row++)
            {
                if (row == col)
                {
                    continue;
                }

                var factor = m[row, col];
                if (factor == 0)
                {
                    continue;
                }

                for (var k = 0; k < 8; k++)
                {
                    m[row, k] -= factor * m[col, k];
                }
            }
        }

        result = new Mat4(
            (float)m[0, 4], (float)m[0, 5], (float)m[0, 6], (float)m[0, 7],
            (float)m[1, 4], (float)m[1, 5], (float)m[1, 6], (float)m[1, 7],
            (float)m[2, 4], (float)m[2, 5], (float)m[2, 6], (float)m[2, 7],
            (float)m[3, 4], (float)m[3, 5], (float)m[3, 6], (float)m[3, 7]);
        return true;
    }
}

