using UnityEngine;

public static class ComponentExtensions
{
    public static bool TryGetComponentInParent<T>(this Component component, out T result) where T : Component
        => component.gameObject.TryGetComponentInParent(out result);
}
