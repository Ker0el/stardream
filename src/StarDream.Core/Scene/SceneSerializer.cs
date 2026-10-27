using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using StarDream.Core.Math;
using StarDream.Core.Meshes;

namespace StarDream.Core.Scene;

/// <summary>
/// Reads and writes the .stardream JSON format. Objects store either their
/// primitive parameters (never edited) or their baked geometry (edited).
/// </summary>
public static class SceneSerializer
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Save(SceneDocument document)
    {
        var dto = new DocumentDto
        {
            Version = CurrentVersion,
            Camera = new CameraDto
            {
                Target = ToArray(document.Camera.Target),
                Distance = document.Camera.Distance,
                Yaw = document.Camera.Yaw,
                Pitch = document.Camera.Pitch,
            },
        };

        foreach (var obj in document.Objects)
        {
            dto.Objects.Add(ToDto(obj));
        }

        return JsonSerializer.Serialize(dto, Options);
    }

    /// <summary>Parses a scene into a fresh document. Throws <see cref="SceneLoadException"/> on bad input.</summary>
    public static SceneDocument Load(string json)
    {
        DocumentDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DocumentDto>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new SceneLoadException("场景文件不是合法 JSON：" + ex.Message, ex);
        }

        if (dto is null)
        {
            throw new SceneLoadException("场景文件为空。");
        }

        if (dto.Version != CurrentVersion)
        {
            throw new SceneLoadException($"不支持的场景版本 {dto.Version}，当前版本为 {CurrentVersion}。");
        }

        var document = new SceneDocument();

        if (dto.Camera is not null)
        {
            document.Camera.Target = ToVec3(dto.Camera.Target);
            document.Camera.Distance = dto.Camera.Distance;
            document.Camera.Yaw = dto.Camera.Yaw;
            document.Camera.Pitch = dto.Camera.Pitch;
        }

        foreach (var objDto in dto.Objects)
        {
            document.Objects.Add(FromDto(objDto));
        }

        document.IsDirty = false;
        return document;
    }

    /// <summary>Loads into an existing document, leaving it untouched when parsing fails.</summary>
    public static void LoadInto(SceneDocument target, string json)
    {
        var loaded = Load(json);

        target.Clear();
        foreach (var obj in loaded.Objects)
        {
            target.Objects.Add(obj);
        }

        target.Camera.Target = loaded.Camera.Target;
        target.Camera.Distance = loaded.Camera.Distance;
        target.Camera.Yaw = loaded.Camera.Yaw;
        target.Camera.Pitch = loaded.Camera.Pitch;
        target.IsDirty = false;
        target.Select(null);
    }

    private static ObjectDto ToDto(SceneObject obj)
    {
        var dto = new ObjectDto
        {
            Id = obj.Id,
            Name = obj.Name,
            Position = ToArray(obj.Position),
            Rotation = ToArray(obj.Rotation),
            Scale = ToArray(obj.Scale),
            Color = obj.ColorHex,
            Visible = obj.Visible,
        };

        if (obj.Baked is not null)
        {
            var positions = new List<float[]>(obj.Baked.Positions.Count);
            foreach (var p in obj.Baked.Positions)
            {
                positions.Add(ToArray(p));
            }

            dto.Mesh = new MeshDto
            {
                Positions = positions,
                Faces = new List<int[]>(obj.Baked.Faces),
            };
        }
        else if (obj.Primitive is not null)
        {
            dto.Primitive = new PrimitiveDto
            {
                Type = obj.Primitive.Type,
                Size = obj.Primitive.Size,
                Radius = obj.Primitive.Radius,
                Height = obj.Primitive.Height,
                Width = obj.Primitive.Width,
                Depth = obj.Primitive.Depth,
                Segments = obj.Primitive.Segments,
                Rings = obj.Primitive.Rings,
            };
        }

        return dto;
    }

    private static SceneObject FromDto(ObjectDto dto)
    {
        SceneObject obj;

        if (dto.Mesh is not null)
        {
            var mesh = new Mesh();
            foreach (var p in dto.Mesh.Positions)
            {
                mesh.Positions.Add(ToVec3(p));
            }

            foreach (var face in dto.Mesh.Faces)
            {
                mesh.Faces.Add(face);
            }

            mesh.RebuildDerived();
            NormalSmoother.Compute(mesh);
            obj = SceneObject.FromBaked(mesh, dto.Name ?? "对象");
        }
        else
        {
            var parameters = dto.Primitive is null
                ? PrimitiveParams.Cube()
                : new PrimitiveParams
                {
                    Type = dto.Primitive.Type,
                    Size = dto.Primitive.Size,
                    Radius = dto.Primitive.Radius,
                    Height = dto.Primitive.Height,
                    Width = dto.Primitive.Width,
                    Depth = dto.Primitive.Depth,
                    Segments = dto.Primitive.Segments,
                    Rings = dto.Primitive.Rings,
                };

            obj = SceneObject.FromPrimitive(parameters, dto.Name ?? "对象");
            obj.Position = ToVec3(dto.Position);
        }

        obj.Position = ToVec3(dto.Position);
        obj.Rotation = ToVec3(dto.Rotation);
        obj.Scale = ToVec3(dto.Scale);
        obj.ColorHex = dto.Color ?? "#C8C8C8";
        obj.Visible = dto.Visible;
        return obj;
    }

    private static float[] ToArray(Vec3 v) => new[] { v.X, v.Y, v.Z };

    private static Vec3 ToVec3(float[]? values)
    {
        if (values is null || values.Length < 3)
        {
            return Vec3.Zero;
        }

        return new Vec3(values[0], values[1], values[2]);
    }

    private sealed class DocumentDto
    {
        public int Version { get; set; }
        public CameraDto? Camera { get; set; }
        public List<ObjectDto> Objects { get; set; } = new();
    }

    private sealed class CameraDto
    {
        public float[]? Target { get; set; }
        public float Distance { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
    }

    private sealed class ObjectDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public float[]? Position { get; set; }
        public float[]? Rotation { get; set; }
        public float[]? Scale { get; set; }
        public string? Color { get; set; }
        public bool Visible { get; set; } = true;
        public PrimitiveDto? Primitive { get; set; }
        public MeshDto? Mesh { get; set; }
    }

    private sealed class PrimitiveDto
    {
        public string Type { get; set; } = "cube";
        public float Size { get; set; } = 1f;
        public float Radius { get; set; } = 0.5f;
        public float Height { get; set; } = 1f;
        public float Width { get; set; } = 1f;
        public float Depth { get; set; } = 1f;
        public int Segments { get; set; } = 16;
        public int Rings { get; set; } = 8;
    }

    private sealed class MeshDto
    {
        public List<float[]> Positions { get; set; } = new();
        public List<int[]> Faces { get; set; } = new();
    }
}
