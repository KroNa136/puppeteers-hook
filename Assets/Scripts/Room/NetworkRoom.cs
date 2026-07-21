using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class NetworkRoom : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClientCurrentIllusionChanged))]
    public int CurrentIllusion;

    [SyncVar]
    public int LinkedRoomId;

    [SerializeField] private float _illusionSetVisibilityChangeDelay = 1f;

    private readonly List<Room> _neighbors = new();
    private readonly List<Door> _doors = new();

    public IEnumerable<Room> Neighbors => _neighbors.ToList();
    public IEnumerable<Door> Doors => _doors.ToList();

    private Collider[] _overlaps;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _overlaps = new Collider[20];
    }

    [Server]
    public void ServerInitialize(Room room)
    {
        var doorways = room.Doorways;
        var neighborDetectors = room.NeighborDetectors;

        var doorLayerMask = room.DoorLayerMask;
        var roomLayerMask = room.RoomLayerMask;

        foreach (var doorway in doorways)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc
            (
                position: doorway.position,
                radius: 0.1f,
                results: _overlaps,
                layerMask: doorLayerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );

            var overlappedDoors = _overlaps
                .Take(overlapCount)
                .Select(o => o.TryGetComponentInParent(out Door door) ? door : null)
                .NonNullItems();

            _doors.AddRange(overlappedDoors);
        }

        foreach (var neighborDetector in neighborDetectors)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc
            (
                position: neighborDetector.position,
                radius: 0.1f,
                results: _overlaps,
                layerMask: roomLayerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Collide
            );

            if (overlapCount == 0 && _doors.Any())
            {
                var correspondingDoor = _doors
                    .Where(d => Vector3.Distance(d.transform.position, neighborDetector.transform.position) < 2f)
                    .MinBy(d => Vector3.Distance(d.transform.position, neighborDetector.transform.position))
                    .FirstOrDefault();

                _ = correspondingDoor.Bind(d => d.ServerLockPermanently());
            }

            var overlappedRooms = _overlaps
                .Take(overlapCount)
                .Select(o => o.TryGetComponent(out Room room) ? room : null)
                .NonNullItems();

            _neighbors.AddRange(overlappedRooms);
        }

        room.LinkedNetworkRoom = this;
        LinkedRoomId = room.Id;

        Invoke(nameof(ServerDisableIllusion), 0.5f);
    }

    [Server]
    public void ServerEnableIllusion()
    {
        if (!isServer)
            return;

        var linkedRoom = FindObjectsByType<Room>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r.Id == LinkedRoomId);

        if (CurrentIllusion >= 0 || linkedRoom == null || linkedRoom.IllusionSets.None())
            return;

        CurrentIllusion = Random.Range(0, linkedRoom.IllusionSets.Count);
    }

    [Server]
    public void ServerDisableIllusion()
    {
        if (!isServer)
            return;

        CurrentIllusion = -1;
    }

    [Client]
    public void OnClientCurrentIllusionChanged(int oldValue, int newValue)
    {
        if (!isClient)
            return;

        var linkedRoom = FindObjectsByType<Room>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r.Id == LinkedRoomId);

        if (linkedRoom == null || linkedRoom.IllusionSets.None())
            return;

        var illusionSets = linkedRoom.IllusionSets;

        for (int i = 0; i < illusionSets.Count; i++)
        {
            bool shouldBeActive = (i == newValue);
            bool isActive = illusionSets[i].activeInHierarchy;

            if (shouldBeActive != isActive)
            {
                var particleSystemSet = linkedRoom.IllusionParticleSystemSets[i];
                var particleSystems = particleSystemSet.GetComponentsInChildren<ParticleSystem>();

                foreach (var particleSystem in particleSystems)
                    particleSystem.Play();

                _ = StartCoroutine(EnableIllusionSetAfterDelay(illusionSets[i], shouldBeActive));
            }
        }
    }

    [Client]
    public IEnumerator EnableIllusionSetAfterDelay(GameObject illusionSet, bool enable)
    {
        if (!isClient)
            yield break;

        yield return new WaitForSeconds(_illusionSetVisibilityChangeDelay);

        illusionSet.SetActive(enable);
    }
}
