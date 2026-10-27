using StarrySky.Core.Scene;

namespace StarrySky.Core.Editing;

/// <summary>Which object is being edited and what is selected inside it.</summary>
public sealed class EditState
{
    public bool Active { get; private set; }
    public SceneObject? Target { get; private set; }
    public EditSelection Selection { get; } = new();

    public void Enter(SceneObject obj)
    {
        Target = obj;
        obj.Bake();
        Active = true;
        Selection.Clear();
        Selection.Mode = ElementMode.Vertex;
    }

    public void Leave()
    {
        Active = false;
        Target = null;
        Selection.Clear();
    }

    public void SwitchMode(ElementMode mode) => Selection.SwitchMode(mode);
}
