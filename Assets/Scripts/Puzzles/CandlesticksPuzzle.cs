using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class CandlesticksPuzzle : Puzzle
{
    private readonly List<Candlestick> _spawnedCandlesticks = new();

    public override bool IsInValidState => isServer && _spawnedCandlesticks.None(c => c.IsLit);

    [Server]
    public override IEnumerator OnServerInitialize()
    {
        if (!isServer)
            yield break;

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        var candlestickSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.Candlestick).Select(sp => sp.transform);
        var noteSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.PuzzleNote).Select(sp => sp.transform);

        foreach (var candlestickSpawnPoint in candlestickSpawnPoints)
        {
            var candlestick = WorldGenerator.Instance.ServerSpawnCandlestick(candlestickSpawnPoint.position, candlestickSpawnPoint.rotation);
            _spawnedCandlesticks.Add(candlestick);
            candlestick.OnServerExtinguished.AddListener(() => StartCoroutine(ServerValidate()));

            yield return new WaitForSeconds(0.3f);
        }

        var noteSpawnPoint = noteSpawnPoints.UnityRandomItem();
        var note = WorldGenerator.Instance.ServerSpawnNote(noteSpawnPoint.position, noteSpawnPoint.rotation);
        yield return new WaitForSeconds(0.3f);
        note.ServerSetCandlesticksPuzzleText();
    }
}
