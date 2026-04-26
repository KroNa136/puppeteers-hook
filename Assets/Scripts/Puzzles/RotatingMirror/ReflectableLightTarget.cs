using Mirror;
using UnityEngine;

public class ReflectableLightTarget : NetworkBehaviour
{
    [SyncVar]
    public bool IsHit;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        IsHit = false;
    }
}
