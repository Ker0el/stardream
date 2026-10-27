namespace StarrySky.Core.Scene;

/// <summary>Parameters that describe a procedurally generated primitive.</summary>
public sealed class PrimitiveParams
{
    public string Type { get; set; } = "cube";

    public float Size { get; set; } = 1f;
    public float Radius { get; set; } = 0.5f;
    public float Height { get; set; } = 1f;
    public float Width { get; set; } = 1f;
    public float Depth { get; set; } = 1f;

    public int Segments { get; set; } = 16;
    public int Rings { get; set; } = 8;

    public static PrimitiveParams Cube(float size = 1f) => new() { Type = "cube", Size = size };

    public static PrimitiveParams Sphere(float radius = 0.5f, int segments = 16, int rings = 8)
        => new() { Type = "sphere", Radius = radius, Segments = segments, Rings = rings };

    public static PrimitiveParams Cylinder(float radius = 0.5f, float height = 1f, int segments = 16)
        => new() { Type = "cylinder", Radius = radius, Height = height, Segments = segments };

    public static PrimitiveParams Plane(float width = 1f, float depth = 1f)
        => new() { Type = "plane", Width = width, Depth = depth };

    public PrimitiveParams Clone() => new()
    {
        Type = Type,
        Size = Size,
        Radius = Radius,
        Height = Height,
        Width = Width,
        Depth = Depth,
        Segments = Segments,
        Rings = Rings,
    };
}
