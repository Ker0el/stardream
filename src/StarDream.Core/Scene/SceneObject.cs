using System;
using StarDream.Core.Math;
using StarDream.Core.Meshes;

namespace StarDream.Core.Scene;

/// <summary>
/// One object in the scene. While <see cref="Primitive"/> is set the mesh is
/// regenerated from those parameters; once the object is edited the mesh is
/// baked and the parameters become read-only history.
/// </summary>
public sealed class SceneObject
{
    private Mesh? _cached;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "对象";
    public Vec3 Position { get; set; } = Vec3.Zero;
    public Vec3 Rotation { get; set; } = Vec3.Zero;
    public Vec3 Scale { get; set; } = Vec3.One;
    public string ColorHex { get; set; } = "#C8C8C8";
    public bool Visible { get; set; } = true;

    public PrimitiveParams? Primitive { get; private set; }
    public Mesh? Baked { get; private set; }

    public bool IsBaked => Baked is not null;

    public static SceneObject FromPrimitive(PrimitiveParams parameters, string name)
    {
        var obj = new SceneObject
        {
            Name = name,
            Primitive = parameters.Clone(),
            Position = new Vec3(0f, Primitives.BaseOffsetY(parameters), 0f),
        };
        return obj;
    }

    /// <summary>Recreates an object that was loaded from a baked mesh.</summary>
    public static SceneObject FromBaked(Mesh mesh, string name)
    {
        return new SceneObject
        {
            Name = name,
            Baked = mesh,
        };
    }

    public Mesh GetMesh()
    {
        if (Baked is not null)
        {
            return Baked;
        }

        if (_cached is not null)
        {
            return _cached;
        }

        var parameters = Primitive ?? PrimitiveParams.Cube();
        _cached = Primitives.Build(parameters);
        NormalSmoother.Compute(_cached);
        return _cached;
    }

    /// <summary>Drops the generated mesh so the next <see cref="GetMesh"/> rebuilds it.</summary>
    public void Invalidate() => _cached = null;

    /// <summary>Freezes the current geometry and turns this object into a plain editable mesh.</summary>
    public Mesh Bake()
    {
        if (Baked is not null)
        {
            return Baked;
        }

        Baked = GetMesh().Clone();
        NormalSmoother.Compute(Baked);
        Primitive = null;
        _cached = null;
        return Baked;
    }

    public Mat4 LocalMatrix() => Mat4.FromTrs(Position, Rotation, Scale);

    public Vec3 Color
    {
        get
        {
            var hex = ColorHex.TrimStart('#');
            if (hex.Length != 6
                || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value))
            {
                return new Vec3(0.78f, 0.78f, 0.78f);
            }

            return new Vec3(
                ((value >> 16) & 0xFF) / 255f,
                ((value >> 8) & 0xFF) / 255f,
                (value & 0xFF) / 255f);
        }
    }

    public SceneObject CloneWithNewId()
    {
        var clone = new SceneObject
        {
            Name = Name,
            Position = Position,
            Rotation = Rotation,
            Scale = Scale,
            ColorHex = ColorHex,
            Visible = Visible,
        };

        if (Baked is not null)
        {
            clone.Baked = Baked.Clone();
        }
        else if (Primitive is not null)
        {
            clone.Primitive = Primitive.Clone();
        }

        return clone;
    }
}
