using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class LobbyPlayer : NetworkBehaviour
{
    public UnityEvent OnReady = new();
   
    [SyncVar(hook = nameof(OnClientIsReadyChanged))]
    public bool IsReady = false;

    private void Awake()
    {
        // Force the object to scene root, in case it was instantiated as a child of something in the scene,
        // since DDOL is only allowed for scene root objects.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    [Command]
    public void CmdPlayerReady()
    {
        IsReady = true;
        LobbyNetworkManager.Instance.ServerCheckAllPlayersReady();
    }

    [Client]
    public void OnClientIsReadyChanged(bool oldValue, bool newValue)
    {
        if (!isClient || !newValue)
            return;

        OnReady.Invoke();
    }
}
