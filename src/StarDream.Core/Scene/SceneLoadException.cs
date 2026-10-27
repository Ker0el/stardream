using System;

namespace StarDream.Core.Scene;

/// <summary>Raised when a scene file cannot be read as a supported document.</summary>
public sealed class SceneLoadException : Exception
{
    public SceneLoadException(string message)
        : base(message)
    {
    }

    public SceneLoadException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
