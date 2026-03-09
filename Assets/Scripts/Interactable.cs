using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : NetworkBehaviour
{
    public UnityEvent<NetworkConnectionToClient> OnInteract = new();

    [SerializeField] private PlayerRole _authorizedPlayerRoles;
    //private Component component;
    //private string methodName;

    public bool IsAuthorizedToInteract(PlayerRole role) => (_authorizedPlayerRoles & role) != 0;

    [Server]
    public void ServerInteract(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        OnInteract.Invoke(conn);
    }

    [Obsolete("This method was implemented for an old singleplayer architecture and won't work. Call Interact() from the server instead.")]
    public void Activate()
    {
        //component.SendMessage(methodName);
    }
}
