using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class PlayerData : NetworkBehaviour
{
    public UnityEvent<PlayerRole> OnRoleAssigned = new();

    public static PlayerData Local { get; private set; } = null;

    [SyncVar(hook = nameof(OnClientRoleChanged))]
    public PlayerRole Role = PlayerRole.None;

    public bool IsLocal { get; private set; } = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    [Client]
    public override void OnStartAuthority()
    {
        Local = this;
        IsLocal = true;
    }

    [Client]
    public void OnClientRoleChanged(PlayerRole oldValue, PlayerRole newValue)
    {
        if (!isClient)
            return;

        OnRoleAssigned.Invoke(newValue);
    }

    private void OnDestroy()
    {
        Local = null;
    }
}
