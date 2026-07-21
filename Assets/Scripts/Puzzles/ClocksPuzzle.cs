using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ClocksPuzzle : Puzzle
{
    [SerializeField] private LayerMask _timeCatcherTrapLayerMask;
    [SerializeField] private float _validateInterval = 0.5f;

    private TimeCatcher _spawnedTimeCatcher;
    private readonly List<TimeCatcherTrapDoor> _spawnedTimeCatcherTrapDoors = new();

    private Collider[] _timeCatcherTrapOverlaps;

    public override bool IsInValidState => isServer && IsInsideTrap(_spawnedTimeCatcher) && _spawnedTimeCatcherTrapDoors.None(td => td.IsOpened);

    private bool _doorsAreUnlocked = false;

    public bool IsInsideTrap(TimeCatcher timeCatcher)
    {
        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: timeCatcher.transform.position,
            radius: 0.01f,
            results: _timeCatcherTrapOverlaps,
            layerMask: _timeCatcherTrapLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Collide
        );

        return overlapCount > 0;
    }

    [Server]
    public override IEnumerator OnServerInitialize()
    {
        if (!isServer)
            yield break;

        _timeCatcherTrapOverlaps = new Collider[20];

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        var timeCatcherSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.TimeCatcher).Select(sp => sp.transform);
        var timeCatcherTrapDoorSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.TimeCatcherTrapDoor).Select(sp => sp.transform);
        var holdableSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.Holdable).Select(sp => sp.transform);
        var noteSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.PuzzleNote).Select(sp => sp.transform);

        var timeCatcherSpawnPoint = timeCatcherSpawnPoints.UnityRandomItem();
        _spawnedTimeCatcher = WorldGenerator.Instance.ServerSpawnTimeCatcher(timeCatcherSpawnPoint.position, timeCatcherSpawnPoint.rotation);
        _spawnedTimeCatcher.NeutralMode = true;

        yield return new WaitForSeconds(0.3f);

        foreach (var spawnPoint in timeCatcherTrapDoorSpawnPoints)
        {
            var timeCatcherTrapDoor = WorldGenerator.Instance.ServerSpawnTimeCatcherTrapDoor(spawnPoint.position, spawnPoint.rotation);
            timeCatcherTrapDoor.ServerOpen();
            _spawnedTimeCatcherTrapDoors.Add(timeCatcherTrapDoor);

            yield return new WaitForSeconds(0.3f);
        }

        foreach (var spawnPoint in holdableSpawnPoints)
        {
            _ = WorldGenerator.Instance.ServerSpawnHoldable(HoldableType.SandClock, spawnPoint.position, spawnPoint.rotation);
            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.3f);

        var noteSpawnPoint = noteSpawnPoints.UnityRandomItem();
        var note = WorldGenerator.Instance.ServerSpawnNote(noteSpawnPoint.position, noteSpawnPoint.rotation);
        yield return new WaitForSeconds(0.3f);
        note.ServerSetClocksPuzzleText();

        _ = StartCoroutine(ValidateCoroutine());
    }

    [Server]
    public IEnumerator ValidateCoroutine()
    {
        if (!isServer)
            yield break;

        while (!_doorsAreUnlocked)
        {
            yield return new WaitForSeconds(_validateInterval);
            _ = StartCoroutine(ServerValidate());
        }
    }

    [Server]
    public override IEnumerator BeforeServerLinkedDoorsUnlock()
    {
        if (!isServer)
            yield break;

        foreach (var timeCatcherTrapDoor in _spawnedTimeCatcherTrapDoors)
        {
            timeCatcherTrapDoor.ServerDisableInteraction();
            yield return new WaitForSeconds(0.3f);
        }
        
        _doorsAreUnlocked = true;
    }
}
