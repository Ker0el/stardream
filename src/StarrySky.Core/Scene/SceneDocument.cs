using System;
using System.Collections.Generic;
using StarrySky.Core.Cameras;
using StarrySky.Core.Math;

namespace StarrySky.Core.Scene;

/// <summary>The document: objects, camera, selection and unsaved-changes state.</summary>
public sealed class SceneDocument
{
    public List<SceneObject> Objects { get; } = new();
    public OrbitCamera Camera { get; } = new();
    public bool IsDirty { get; set; }
    public string? FilePath { get; set; }
    public SceneObject? Selected { get; private set; }

    public event Action? SelectionChanged;

    public void Select(SceneObject? obj)
    {
        if (ReferenceEquals(Selected, obj))
        {
            return;
        }

        Selected = obj;
        SelectionChanged?.Invoke();
    }

    public SceneObject AddPrimitive(PrimitiveParams parameters)
    {
        var name = NextName(parameters.Type);
        var obj = SceneObject.FromPrimitive(parameters, name);
        Objects.Add(obj);
        IsDirty = true;
        Select(obj);
        return obj;
    }

    public SceneObject Duplicate(SceneObject source)
    {
        var clone = source.CloneWithNewId();
        clone.Name = source.Name + "_copy";
        clone.Position = clone.Position + new Vec3(0.5f, 0f, 0f);
        Objects.Add(clone);
        IsDirty = true;
        Select(clone);
        return clone;
    }

    /// <summary>Adds an object built elsewhere, e.g. pasted from the clipboard.</summary>
    public SceneObject Add(SceneObject obj)
    {
        Objects.Add(obj);
        IsDirty = true;
        Select(obj);
        return obj;
    }

    public void Remove(SceneObject obj)
    {
        if (Objects.Remove(obj))
        {
            IsDirty = true;
            if (ReferenceEquals(Selected, obj))
            {
                Select(null);
            }
        }
    }

    public void SetVisible(SceneObject obj, bool visible)
    {
        if (obj.Visible == visible)
        {
            return;
        }

        obj.Visible = visible;
        IsDirty = true;
    }

    public void Clear()
    {
        Objects.Clear();
        IsDirty = false;
        FilePath = null;
        Select(null);
        Camera.Target = new Vec3(0f, 0.5f, 0f);
        Camera.Distance = 12f;
        Camera.Yaw = 0.6f;
        Camera.Pitch = 0.4f;
    }

    public int TriangleCount
    {
        get
        {
            var total = 0;
            foreach (var obj in Objects)
            {
                total += obj.GetMesh().Triangles.Count;
            }

            return total;
        }
    }

    private string NextName(string type)
    {
        var display = type switch
        {
            "sphere" => "球体",
            "cylinder" => "圆柱",
            "plane" => "平面",
            _ => "立方体",
        };

        var index = 1;
        while (Objects.Exists(o => o.Name == $"{display}.{index:D3}"))
        {
            index++;
        }

        return $"{display}.{index:D3}";
    }
}
