using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class Room : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClientCurrentIllusionChanged))]
    public int CurrentIllusion;

    public RoomType Type { get; private set; } = RoomType.None;

    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private List<Transform> _doorways;
    [SerializeField] private List<Transform> _neighborDetectors;

    [Space]

    [SerializeField] private LayerMask _doorLayerMask;
    [SerializeField] private LayerMask _roomLayerMask;

    [Space]

    [SerializeField] private List<GameObject> _illusionSets;

    private readonly List<Room> _neighbors = new();
    private readonly List<Door> _doors = new();

    public Vector3 PlayerSpawnPosition => _playerSpawnPoint.TryGet(sp => sp.position, out var position) ? position : transform.position;
    public IEnumerable<Room> Neighbors => _neighbors.ToList();
    public IEnumerable<Door> Doors => _doors.ToList();

    private Collider[] _overlaps;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        CurrentIllusion = -1;

        _overlaps = new Collider[20];
    }

    [Server]
    public void ServerSetType(RoomType type)
    {
        if (!isServer)
            return;

        Type = type;
    }

    [Server]
    public void ServerInitialize()
    {
        foreach (var doorway in _doorways)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc
            (
                position: doorway.position,
                radius: 0.1f,
                results: _overlaps,
                layerMask: _doorLayerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );

            var overlappedDoors = _overlaps
                .Take(overlapCount)
                .Select(o => o.TryGetComponent(out Door door) ? door : null)
                .NonNullItems();

            _doors.AddRange(overlappedDoors);
        }

        foreach (var neighborDetector in _neighborDetectors)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc
            (
                position: neighborDetector.position,
                radius: 0.1f,
                results: _overlaps,
                layerMask: _roomLayerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Collide
            );

            if (overlapCount == 0 && _doors.Any())
            {
                var closestDoor = _doors.MinBy(door => Vector3.Distance(door.transform.position, neighborDetector.transform.position)).FirstOrDefault();
                _ = closestDoor.Bind(d => d.ServerLock());
            }

            var overlappedRooms = _overlaps
                .Take(overlapCount)
                .Select(o => o.TryGetComponent(out Room room) ? room : null)
                .NonNullItems();

            _neighbors.AddRange(overlappedRooms);
        }
    }

    [Server]
    public void ServerEnableIllusion()
    {
        if (!isServer)
            return;

        if (CurrentIllusion >= 0 || _illusionSets.None())
            return;

        CurrentIllusion = Random.Range(0, _illusionSets.Count);
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

        if (oldValue == newValue)
            return;

        if (_illusionSets.None())
            return;

        foreach (var illusionSet in _illusionSets)
        {
            if (illusionSet.activeInHierarchy)
                illusionSet.SetActive(false);
        }

        if (newValue < 0 || newValue >= _illusionSets.Count)
            return;

        _illusionSets[newValue].SetActive(true);
    }
}
