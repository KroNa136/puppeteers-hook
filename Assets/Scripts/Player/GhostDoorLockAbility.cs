using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class GhostDoorLockAbility : GhostAbility
{
    [SerializeField] private LayerMask _roomLayerMask;

    private Collider[] _overlaps;
    private List<Door> _doorsToLock = new();

    private readonly Dictionary<Door, Coroutine> _closeAndLockCoroutines = new();

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        base.OnStartServer();

        _overlaps = new Collider[20];
    }

    [Server]
    public override void ServerDoActivation()
    {
        if (!isServer)
            return;

        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: transform.position,
            radius: 0.01f,
            results: _overlaps,
            layerMask: _roomLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Collide
        );

        // TODO: think of a way to determine one exact room (though I guess a sphere with R = 0.01 works well enough)
        _doorsToLock = _overlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems()
            .SelectMany(r => r.Doors)
            .ToList();

        foreach (var door in _doorsToLock)
        {
            if (door.IsLocked)
                continue;

            if (door.IsOpened)
                _closeAndLockCoroutines[door] = StartCoroutine(ServerCloseAndLock(door));
            else
                door.ServerLock();
        }
    }

    [Server]
    public IEnumerator ServerCloseAndLock(Door door)
    {
        if (!isServer)
            yield break;

        if (!door.IsOpened)
        {
            door.ServerLock();
            yield break;
        }

        door.ServerClose();

        yield return new WaitForSeconds(0.1f);

        while (door.IsAnimating)
            yield return null;

        door.ServerLock();

        _ = _closeAndLockCoroutines.Remove(door);
    }

    [Server]
    public override void ServerDoDeactivation()
    {
        if (!isServer)
            return;

        StopAllCoroutines();
        _closeAndLockCoroutines.Clear();

        foreach (var door in _doorsToLock)
        {
            if (door.IsLocked)
                door.ServerUnlock();
        }

        _doorsToLock.Clear();
    }
}
