using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class GhostIllusionAbility : GhostAbility
{
    [SerializeField] private LayerMask _roomLayerMask;

    private Collider[] _overlaps;
    private Collider[] _overlaps2;
    private List<NetworkRoom> _networkRoomsToEnableIllusionIn = new();

    public List<NetworkRoom> NetworkRoomsWithIllusion => _networkRoomsToEnableIllusionIn.ToList();

    protected override void PlayActivationSound()
    {
        _ = _audioController.Bind(c => c.PlayIllusionAbilityActivationSound());
    }

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

        bool isInRoomWithIllusionSets = _overlaps2
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems()
            .Any(r => r.IllusionSets.Any());

        CanBeActivated = isInRoomWithIllusionSets;
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

        _networkRoomsToEnableIllusionIn = _overlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems()
            .Select(r => r.LinkedNetworkRoom)
            .ToList();

        foreach (var room in _networkRoomsToEnableIllusionIn)
            room.ServerEnableIllusion();
    }

    [Server]
    public override void ServerDoDeactivation()
    {
        if (!isServer)
            return;

        foreach (var room in _networkRoomsToEnableIllusionIn)
            room.ServerDisableIllusion();

        _networkRoomsToEnableIllusionIn.Clear();
    }
}
