using Mirror;
using System;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : NetworkBehaviour
{
    public UnityEvent<NetworkConnectionToClient> OnInteract = new();
    public UnityEvent OnPredictInteraction = new();
    public UnityEvent OnFailInteraction = new();

    [SerializeField] private PlayerRole _authorizedPlayerRoles;
    //private Component component;
    //private string methodName;

    public bool IsAuthorizedToInteract(PlayerRole role) => (_authorizedPlayerRoles & role) != 0;

    [Client]
    public void ClientPredictInteraction()
    {
        OnPredictInteraction.Invoke();
    }

    [Server]
    public void ServerInteract(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        OnInteract.Invoke(conn);
    }

    [TargetRpc]
    public void TargetRpcFailInteraction(NetworkConnectionToClient conn)
    {
        OnFailInteraction.Invoke();
    }

    [Obsolete("This method was implemented for an old singleplayer architecture and won't work. Call Interact() from the server instead.")]
    public void Activate()
    {
        //component.SendMessage(methodName);
    }
}
