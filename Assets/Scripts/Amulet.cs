using Mirror;
using UnityEngine;

public class Amulet : NetworkBehaviour
{
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
