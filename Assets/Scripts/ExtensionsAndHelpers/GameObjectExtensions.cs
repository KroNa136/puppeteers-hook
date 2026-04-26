using UnityEngine;

public static class GameObjectExtensions
{
    public static bool TryGetComponentInParent<T>(this GameObject gameObj, out T component) where T : Component
    {
        Transform parent = gameObj.transform.parent;

        if (parent != null)
        {
            return parent.TryGetComponent(out component);
        }
        else
        {
            component = null;
            return false;
        }
    }
}
