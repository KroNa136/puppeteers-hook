using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class GhostIllusionAbility : GhostAbility
{
    [SerializeField] private LayerMask _roomLayerMask;

    private Collider[] _overlaps;
    private Collider[] _overlaps2;
    private List<Room> _roomsToEnableIllusionIn = new();

    public List<Room> RoomsWithIllusion => _roomsToEnableIllusionIn.ToList();

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        base.OnStartServer();

        _overlaps = new Collider[20];
        _overlaps2 = new Collider[20];

        GameManager.OnServerMainPhaseStarted.AddListener(ServerStartMainPhase);
    }

    [Server]
    public void ServerStartMainPhase()
    {
        if (!isServer)
            return;

        TickSystem.Instance.OnTick.AddListener(ServerTick);
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: transform.position,
            radius: 0.01f,
            results: _overlaps2,
            layerMask: _roomLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Collide
        );

        bool isNotInPuzzleRoom = _overlaps2
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems()
            .None(r => r.Type is RoomType.EmergencyExit or RoomType.StatuesPuzzleRoom or RoomType.RotatingMirrorsPuzzleRoom);

        CanBeActivated = isNotInPuzzleRoom;
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

        _roomsToEnableIllusionIn = _overlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems()
            .ToList();

        foreach (var room in _roomsToEnableIllusionIn)
            room.ServerEnableIllusion();
    }

    [Server]
    public override void ServerDoDeactivation()
    {
        if (!isServer)
            return;

        foreach (var room in _roomsToEnableIllusionIn)
            room.ServerDisableIllusion();

        _roomsToEnableIllusionIn.Clear();
    }
}
