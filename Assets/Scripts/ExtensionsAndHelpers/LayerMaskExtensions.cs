using UnityEngine;

public static class LayerMaskExtensions
{
    public static bool ContainsLayer(this LayerMask layerMask, int layer)
        => ((1 << layer) & layerMask) > 0;

    public static bool IsInLayerMask(this GameObject gameObject, LayerMask layerMask)
        => ((1 << gameObject.layer) & layerMask) > 0;

    public static bool IsNotInLayerMask(this GameObject gameObject, LayerMask layerMask)
        => !gameObject.IsInLayerMask(layerMask);
}
