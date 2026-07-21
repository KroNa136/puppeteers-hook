using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class Amulet : NetworkBehaviour
{
    [SerializeField] private AmuletAudioController _audioController;

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
            _ = StartCoroutine(PlayPickUpSoundAndDestroy());
    }

    [Server]
    public IEnumerator PlayPickUpSoundAndDestroy()
    {
        if (!isServer)
            yield break;

        RpcDisable();
        ClientDisable();

        RpcPlayPickUpSound();
        ClientPlayPickUpSound();

        if (_audioController != null)
        {
            while (_audioController.IsPlaying)
                yield return null;
        }

        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    public void RpcDisable()
    {
        if (isServer)
            return;

        ClientDisable();
    }

    [Client]
    public void ClientDisable()
    {
        if (!isClient)
            return;

        if (TryGetComponent(out Interactable interactable))
            interactable.enabled = false;

        var colliders = GetComponentsInChildren<Collider>();

        if (TryGetComponent(out Collider ownCollider))
            colliders = colliders.Prepend(ownCollider).ToArray();

        foreach (var collider in colliders)
            collider.enabled = false;

        var renderers = GetComponentsInChildren<Renderer>();

        if (TryGetComponent(out Renderer ownRenderer))
            renderers = renderers.Prepend(ownRenderer).ToArray();

        foreach (var renderer in renderers)
            renderer.enabled = false;
    }

    [ClientRpc]
    public void RpcPlayPickUpSound()
    {
        if (isServer)
            return;

        ClientPlayPickUpSound();
    }

    [Client]
    public void ClientPlayPickUpSound()
    {
        if (!isClient)
            return;

        _ = _audioController.Bind(c => c.PlayPickUpSound());
    }
}
