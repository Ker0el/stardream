using StarDream.Core.Math;

namespace StarDream.Core.Cameras;

/// <summary>
/// Maya style orbit camera: tumble, track and dolly around a target point.
/// Angles are stored in radians; pitch is clamped to ±89° to avoid gimbal flip.
/// </summary>
public sealed class OrbitCamera
{
    public const float MinDistance = 0.5f;
    public const float MaxDistance = 200f;
    public const float MaxPitch = 89f * MathUtil.DegToRad;

    public Vec3 Target { get; set; } = new(0f, 0.5f, 0f);
    public float Distance { get; set; } = 12f;
    public float Yaw { get; set; } = 0.6f;
    public float Pitch { get; set; } = 0.4f;
    public float FovYDegrees { get; set; } = 45f;

    public Vec3 Eye
    {
        get
        {
            var cosPitch = MathF.Cos(Pitch);
            return Target + new Vec3(
                Distance * cosPitch * MathF.Sin(Yaw),
                Distance * MathF.Sin(Pitch),
                Distance * cosPitch * MathF.Cos(Yaw));
        }
    }

    public void Orbit(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch = MathUtil.Clamp(Pitch + deltaPitch, -MaxPitch, MaxPitch);
    }

    /// <summary>Tracks the target in the camera plane; deltas are in pixels.</summary>
    public void Pan(float dxPixels, float dyPixels, int viewportHeight)
    {
        if (viewportHeight <= 0)
        {
            return;
        }

        var scale = 2f * Distance * MathF.Tan(FovYDegrees * MathUtil.DegToRad * 0.5f) / viewportHeight;
        var view = GetView();

        // View matrix rows are the camera basis: row 1 = right, row 2 = up.
        var right = new Vec3(view.M11, view.M12, view.M13);
        var up = new Vec3(view.M21, view.M22, view.M23);

        Target -= right * (dxPixels * scale);
        Target += up * (dyPixels * scale);
    }

    public void Dolly(float factor)
    {
        if (!float.IsFinite(factor) || factor <= 0f)
        {
            return;
        }

        Distance = MathUtil.Clamp(Distance * factor, MinDistance, MaxDistance);
    }

    /// <summary>Moves the target to <paramref name="center"/> and backs off so a sphere of that radius fits.</summary>
    public void Frame(Vec3 center, float radius)
    {
        Target = center;
        var safeRadius = MathF.Max(radius, 0.1f);
        var halfFov = FovYDegrees * MathUtil.DegToRad * 0.5f;
        Distance = MathUtil.Clamp(safeRadius / MathF.Sin(halfFov), MinDistance, MaxDistance);
    }

    public Mat4 GetView() => Mat4.LookAt(Eye, Target, Vec3.UnitY);

    public Mat4 GetProjection(float aspect)
    {
        var safeAspect = aspect > 1e-4f ? aspect : 1f;
        return Mat4.PerspectiveFov(FovYDegrees * MathUtil.DegToRad, safeAspect, 0.1f, 500f);
    }

    /// <summary>Builds a world space ray through a pixel, with the origin on the near plane.</summary>
    public Ray ScreenToRay(Vec2 screenPixels, int viewportWidth, int viewportHeight)
    {
        var width = MathF.Max(1f, viewportWidth);
        var height = MathF.Max(1f, viewportHeight);

        var ndcX = 2f * screenPixels.X / width - 1f;
        var ndcY = 1f - 2f * screenPixels.Y / height;

        var viewProjection = GetProjection(width / height) * GetView();
        if (!viewProjection.TryInvert(out var inverse))
        {
            return new Ray(Eye, (Target - Eye).Normalized());
        }

        var nearPoint = inverse.TransformPoint(new Vec3(ndcX, ndcY, 0f));
        var farPoint = inverse.TransformPoint(new Vec3(ndcX, ndcY, 1f));
        var direction = (farPoint - nearPoint).Normalized();

        return new Ray(nearPoint, direction);
    }
}
