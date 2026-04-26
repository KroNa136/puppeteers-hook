using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class StatuesPuzzle : Puzzle
{
    private float _targetStatueAngle;
    private readonly List<Statue> _spawnedStatues = new();

    public override bool IsInValidState => isServer && _spawnedStatues.All(s => s.transform.eulerAngles.y == _targetStatueAngle);

    [Server]
    public override void OnServerInitialize()
    {
        if (!isServer)
            return;

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        var statueSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.Statue).Select(sp => sp.transform);
        var windRoseSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.WindRose).Select(sp => sp.transform);
        var noteSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.PuzzleNote).Select(sp => sp.transform);

        float statueRotationAngleDelta = LobbyNetworkManager.Instance.StatuePrefab.GetComponent<Statue>().RotationAngleDelta;

        float angle = statueRotationAngleDelta;
        int states = 1;

        while (angle % 360f != 0)
        {
            angle += statueRotationAngleDelta;
            states++;
        }

        _targetStatueAngle = GetRandomRotationAngle(states, statueRotationAngleDelta);

        foreach (var spawnPoint in statueSpawnPoints)
        {
            float spawnAngle = GetRandomRotationAngle(states, statueRotationAngleDelta);
            spawnPoint.rotation = Quaternion.Euler(0f, spawnAngle, 0f);

            var statue = WorldGenerator.Instance.ServerSpawnStatue(spawnPoint.position, spawnPoint.rotation);
            _spawnedStatues.Add(statue);
            statue.OnServerFinishedRotation.AddListener(ServerValidate);
        }

        float windRoseAngle = GetRandomRotationAngle(states, statueRotationAngleDelta);

        foreach (var spawnPoint in windRoseSpawnPoints)
            spawnPoint.rotation = Quaternion.Euler(0f, windRoseAngle, 0f);

        var windRoseSpawnPoint = windRoseSpawnPoints.UnityRandomItem();
        _ = WorldGenerator.Instance.ServerSpawnWindRose(windRoseSpawnPoint.position, windRoseSpawnPoint.rotation);

        var noteSpawnPoint = noteSpawnPoints.UnityRandomItem();
        var note = WorldGenerator.Instance.ServerSpawnNote(noteSpawnPoint.position, noteSpawnPoint.rotation);
        note.ServerSetStatuesPuzzleText(_targetStatueAngle, windRoseAngle);
    }

    [Server]
    public override void BeforeServerLinkedDoorsUnlock()
    {
        if (!isServer)
            return;

        foreach (var statue in _spawnedStatues)
            statue.ServerDisableInteraction();
    }

    private float GetRandomRotationAngle(int stateCount, float rotationAngleDelta)
        => Random.Range(0, stateCount) * rotationAngleDelta;
}
