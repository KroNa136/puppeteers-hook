using Mirror;
using UnityEngine;

public class Amulet : NetworkBehaviour
{
    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        if (TryGetComponent(out Interactable interactable))
            interactable.OnInteract.AddListener(ServerPickUp);
    }

    [Server]
    public void ServerPickUp(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        if (!conn.identity.TryGetComponent(out Inventory inventory))
            return;

        if (inventory.ServerPickUpAmulet())
            NetworkServer.Destroy(gameObject);
    }
}
