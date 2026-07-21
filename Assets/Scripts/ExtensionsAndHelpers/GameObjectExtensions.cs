using UnityEngine;

public static class GameObjectExtensions
{
    public static bool TryGetComponentInParent<T>(this GameObject gameObj, out T component) where T : Component
    {
        Transform parent = gameObj.transform.parent;

        component = null;
        return parent != null && parent.TryGetComponent(out component);
    }

    public static bool TryGetComponentInSecondParent<T>(this GameObject gameObj, out T component) where T : Component
    {
        Transform parent = gameObj.transform.parent;

        component = null;

        if (parent != null)
        {
            Transform secondParent = parent.parent;
            return secondParent != null && secondParent.TryGetComponent(out component);
        }
        else
        {
            return false;
        }
    }
}
