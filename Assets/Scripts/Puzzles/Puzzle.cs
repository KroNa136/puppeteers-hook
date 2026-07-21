using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public abstract class Puzzle : NetworkBehaviour
{
    public PuzzleType Type { get; private set; } = PuzzleType.None;

    [SerializeField] private LayerMask _roomLayerMask;
    protected List<Door> _linkedDoors = new();

    private Collider[] _overlaps;

    public abstract bool IsInValidState { get; }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _overlaps = new Collider[20];
    }

    [Server]
    public void ServerSetType(PuzzleType type)
    {
        if (!isServer)
            return;

        Type = type;
    }

    [Server]
    public IEnumerator ServerInitialize()
    {
        if (!isServer)
            yield break;

        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: transform.position,
            radius: 0.01f,
            results: _overlaps,
            layerMask: _roomLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Collide
        );

        _linkedDoors = _overlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems()
            .Select(r => r.LinkedNetworkRoom)
            .SelectMany(r => r.Doors)
            .Where(d => d.IsLocked)
            .ToList();

        yield return OnServerInitialize();
    }

    [Server]
    public IEnumerator ServerValidate()
    {
        if (!isServer)
            yield break;

        if (!IsInValidState)
            yield break;

        yield return BeforeServerLinkedDoorsUnlock();

        foreach (var door in _linkedDoors)
        {
            door.ServerUnlock();
            yield return new WaitForSeconds(0.2f);
        }
    }

    public abstract IEnumerator OnServerInitialize();
    public virtual IEnumerator BeforeServerLinkedDoorsUnlock()
    {
        yield return null;
    }
}
