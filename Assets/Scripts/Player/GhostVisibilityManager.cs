using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class GhostVisibilityManager : NetworkBehaviour
{
    [SerializeField] private float _highVisibilityAlpha = 1f;
    [SerializeField] private float _lowVisibilityAlpha = 0.5f;
    [SerializeField] private float _zeroVisibilityAlpha = 0f;

    private IEnumerable<Renderer> _renderers;

    private void Start()
    {
        _renderers = GetComponentsInChildren<Renderer>().AsEnumerable();

        if (TryGetComponent(out Renderer renderer))
            _renderers = _renderers.Prepend(renderer);
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        Invoke(nameof(ServerSetLowVisibility), 1f);
    }

    [Server]
    public void ServerSetHighVisibility()
    {
        if (!isServer)
            return;

        RpcSetAlpha(_highVisibilityAlpha);
    }

    [Server]
    public void ServerSetLowVisibility()
    {
        if (!isServer)
            return;

        RpcSetAlpha(_lowVisibilityAlpha);
    }

    [Server]
    public void ServerSetZeroVisibility()
    {
        if (!isServer)
            return;

        RpcSetAlpha(_zeroVisibilityAlpha);

        var fearReceivers = FindObjectsByType<FearManager>(FindObjectsSortMode.None).Where(f => f.ServerIsAfraidOf(transform));

        foreach (var fearReceiver in fearReceivers)
            fearReceiver.ServerStopBeingAfraidOf(transform);
    }

    [ClientRpc]
    public void RpcSetAlpha(float alpha)
    {
        float clampedAlpha = Mathf.Clamp01(alpha);

        _renderers
            .NonNullItems()
            .SelectMany(rend => rend.materials)
            .NonNullItems()
            .ForEach(mat =>
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.color = new
                    (
                        r: mat.color.r,
                        g: mat.color.g,
                        b: mat.color.b,
                        a: alpha
                    );
                }
            });
    }
}
