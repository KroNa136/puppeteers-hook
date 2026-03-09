using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class LobbyNotifier : NetworkBehaviour
{
    public static LobbyNotifier Instance;

    public static UnityEvent OnAllLobbyPlayersSpawned = new();
    public static UnityEvent OnLobbyPlayerLeft = new();
    public static UnityEvent OnSceneReady = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    [ClientRpc]
    public void RpcAllLobbyPlayersSpawned()
    {
        OnAllLobbyPlayersSpawned.Invoke();
    }

    [ClientRpc]
    public void RpcLobbyPlayerLeft()
    {
        OnLobbyPlayerLeft.Invoke();
    }

    [ClientRpc]
    public void RpcSceneReady()
    {
        OnSceneReady.Invoke();
    }
}
