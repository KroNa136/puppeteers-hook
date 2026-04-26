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
    public void ServerInitialize()
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

        _linkedDoors = _overlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems()
            .SelectMany(r => r.Doors)
            .Where(d => d.IsLocked)
            .ToList();

        OnServerInitialize();
    }

    [Server]
    public void ServerValidate()
    {
        if (!isServer)
            return;

        if (!IsInValidState)
            return;

        BeforeServerLinkedDoorsUnlock();

        foreach (var door in _linkedDoors)
            door.ServerUnlock();
    }

    public abstract void OnServerInitialize();
    public virtual void BeforeServerLinkedDoorsUnlock() { }
}
