using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class HoldablesPuzzle : Puzzle
{
    [SerializeField] private int _holdableCount = 3;

    [Space]

    [SerializeField] private List<HoldablePlacementTarget> _holdablePlacementTargets;
    [SerializeField] private LayerMask _holdablePlacementTargetLayerMask;
    [SerializeField] private float _validateInterval = 0.5f;

    private readonly List<Holdable> _spawnedHoldables = new();
    private readonly List<HoldablePlacementTarget> _usedHoldablePlacementTargets = new();
    private Collider[] _targetOverlaps;

    public override bool IsInValidState => isServer && _spawnedHoldables.All(IsPlacedCorrectly);

    private bool _doorsAreUnlocked = false;

    public bool IsPlacedCorrectly(Holdable holdable)
    {
        if (holdable.IsHeld)
            return false;

        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: holdable.transform.position,
            radius: 0.01f,
            results: _targetOverlaps,
            layerMask: _holdablePlacementTargetLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Collide
        );

        return _targetOverlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out HoldablePlacementTarget target) ? target : null)
            .NonNullItems()
            .Select(t => t.HoldableType)
            .Contains(holdable.Type);
    }

    [Server]
    public override IEnumerator OnServerInitialize()
    {
        if (!isServer)
            yield break;

        _targetOverlaps = new Collider[20];

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        var holdableSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.Holdable).ToList();
        var noteSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.PuzzleNote).Select(sp => sp.transform);

        // intentionally leaving out SandClock because it is used in a different puzzle
        List<HoldableType> holdableTypes = new() { HoldableType.Skull, HoldableType.Globe, HoldableType.Crystal };
        List<HoldablePlacementTarget> holdablePlacementTargetsCopy = new(_holdablePlacementTargets);

        for (int i = 0; i < _holdableCount; i++)
        {
            var randomSpawnPoint = holdableSpawnPoints.UnityRandomItem();
            _ = holdableSpawnPoints.Remove(randomSpawnPoint);

            var randomHoldableType = holdableTypes.UnityRandomItem();
            _ = holdableTypes.Remove(randomHoldableType);

            var holdable = WorldGenerator.Instance.ServerSpawnHoldable(randomHoldableType, randomSpawnPoint.transform.position, randomSpawnPoint.transform.rotation);
            _spawnedHoldables.Add(holdable);

            var randomPlacementTarget = holdablePlacementTargetsCopy.UnityRandomItem();
            _ = holdablePlacementTargetsCopy.Remove(randomPlacementTarget);
            randomPlacementTarget.HoldableType = randomHoldableType;
            _usedHoldablePlacementTargets.Add(randomPlacementTarget);

            yield return new WaitForSeconds(0.3f);
        }

        var noteSpawnPoint = noteSpawnPoints.UnityRandomItem();
        var note = WorldGenerator.Instance.ServerSpawnNote(noteSpawnPoint.position, noteSpawnPoint.rotation);
        yield return new WaitForSeconds(0.3f);
        note.ServerSetHoldablesPuzzleText(_usedHoldablePlacementTargets);

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

        foreach (var holdable in _spawnedHoldables)
        {
            holdable.ServerDisableInteraction();
            yield return new WaitForSeconds(0.3f);
        }

        _doorsAreUnlocked = true;
    }
}
