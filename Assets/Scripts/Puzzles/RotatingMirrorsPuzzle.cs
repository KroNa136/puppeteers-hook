using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class RotatingMirrorsPuzzle : Puzzle
{
    private readonly List<RotatingMirror> _spawnedRotatingMirrors = new();
    private ReflectableLightTarget _spawnedReflectableLightTarget;

    public override bool IsInValidState => isServer && _spawnedReflectableLightTarget.IsHit;

    [Server]
    public override void OnServerInitialize()
    {
        if (!isServer)
            return;

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        var reflectableLightSourceSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.ReflectableLightSource).Select(sp => sp.transform);
        var reflectableLightTargetSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.ReflectableLightTarget).Select(sp => sp.transform);
        var rotatingMirrorSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.RotatingMirror).Select(sp => sp.transform);
        var noteSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.PuzzleNote).Select(sp => sp.transform);

        var reflectableLightSourceSpawnPoint = reflectableLightSourceSpawnPoints.UnityRandomItem();
        _ = WorldGenerator.Instance.ServerSpawnReflectableLightSource(reflectableLightSourceSpawnPoint.position, reflectableLightSourceSpawnPoint.rotation);

        var reflectableLightTargetSpawnPoint = reflectableLightTargetSpawnPoints.UnityRandomItem();
        _spawnedReflectableLightTarget = WorldGenerator.Instance.ServerSpawnReflectableLightTarget(reflectableLightTargetSpawnPoint.position, reflectableLightTargetSpawnPoint.rotation);

        float rotatingMirrorRotationAngleDelta = LobbyNetworkManager.Instance.RotatingMirrorPrefab.GetComponent<RotatingMirror>().RotationAngleDelta;

        float angle = rotatingMirrorRotationAngleDelta;
        int states = 1;

        while (angle % 360f != 0)
        {
            angle += rotatingMirrorRotationAngleDelta;
            states++;
        }

        foreach (var spawnPoint in rotatingMirrorSpawnPoints)
        {
            float spawnAngle = GetRandomRotationAngle(states, rotatingMirrorRotationAngleDelta);
            spawnPoint.rotation = Quaternion.Euler(0f, spawnAngle, 0f);

            var rotatingMirror = WorldGenerator.Instance.ServerSpawnRotatingMirror(spawnPoint.position, spawnPoint.rotation);
            _spawnedRotatingMirrors.Add(rotatingMirror);
            rotatingMirror.OnServerFinishedRotation.AddListener(ServerValidate);
        }

        var noteSpawnPoint = noteSpawnPoints.UnityRandomItem();
        var note = WorldGenerator.Instance.ServerSpawnNote(noteSpawnPoint.position, noteSpawnPoint.rotation);
        note.ServerSetRotatingMirrorsPuzzleText();
    }

    [Server]
    public override void BeforeServerLinkedDoorsUnlock()
    {
        if (!isServer)
            return;

        foreach (var rotatingMirror in _spawnedRotatingMirrors)
            rotatingMirror.ServerDisableInteraction();
    }

    private float GetRandomRotationAngle(int stateCount, float rotationAngleDelta)
        => Random.Range(0, stateCount) * rotationAngleDelta;
}
