using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class Candlestick : NetworkBehaviour
{
    public UnityEvent OnServerExtinguished = new();

    [SerializeField] private Renderer _litRenderer;
    [SerializeField] private Renderer _extinguishedRenderer;
    [SerializeField] private Light _light;
    // [SerializeField] private CandlestickAudioController _audioController;

    [SyncVar(hook = nameof(OnClientIsLitChanged))]
    public bool IsLit;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        IsLit = true;

        if (TryGetComponent(out Interactable interactable))
            interactable.OnInteract.AddListener(ServerExtinguish);
    }

    [Server]
    public void ServerExtinguish(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        if (!IsLit)
            return;

        IsLit = false;
        OnServerExtinguished.Invoke();

        DisableInteraction();
        RpcDisableInteraction();
    }

    [ClientRpc]
    public void RpcDisableInteraction()
    {
        if (isServer)
            return;

        DisableInteraction();
    }

    private void DisableInteraction()
    {
        if (TryGetComponent(out Interactable interactable))
            interactable.enabled = false;
    }

    [Client]
    public void OnClientIsLitChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;

        _litRenderer.enabled = newValue;
        _extinguishedRenderer.enabled = !newValue;

        _ = _light.Bind(l => l.enabled = newValue);

        // _ = _audioController.Bind(newValue ? c => c.PlayPutOutSound() : c => c.PlayLightSound());
    }
}
