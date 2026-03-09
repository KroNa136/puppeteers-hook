using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : NetworkBehaviour
{
    public UnityEvent PickedUpAmulet = new();
    public UnityEvent SpentAmulet = new();

    [SyncVar(hook = nameof(OnClientAmuletOwnershipChanged))]
    public bool HasAmulet;

    [Server]
    public bool ServerPickUpAmulet()
    {
        if (!isServer || HasAmulet)
            return false;

        HasAmulet = true;
        return true;
    }

    [Server]
    public bool ServerSpendAmulet()
    {
        if (!isServer || !HasAmulet)
            return false;

        HasAmulet = false;
        return true;
    }

    [Client]
    public void OnClientAmuletOwnershipChanged(bool oldValue, bool newValue)
    {
        if (!isClient || !isOwned)
            return;

        if (newValue)
            PickedUpAmulet.Invoke();
        else
            SpentAmulet.Invoke();
    }
}
