using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class CandlesticksPuzzle : Puzzle
{
    private readonly List<Candlestick> _spawnedCandlesticks = new();

    public override bool IsInValidState => isServer && _spawnedCandlesticks.None(c => c.IsLit);

    [Server]
    public override void OnServerInitialize()
    {
        if (!isServer)
            return;

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        var candlestickSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.Candlestick).Select(sp => sp.transform);
        var noteSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.PuzzleNote).Select(sp => sp.transform);

        foreach (var candlestickSpawnPoint in candlestickSpawnPoints)
        {
            var candlestick = WorldGenerator.Instance.ServerSpawnCandlestick(candlestickSpawnPoint.position, candlestickSpawnPoint.rotation);
            _spawnedCandlesticks.Add(candlestick);
            candlestick.OnServerExtinguished.AddListener(ServerValidate);
        }

        var noteSpawnPoint = noteSpawnPoints.UnityRandomItem();
        var note = WorldGenerator.Instance.ServerSpawnNote(noteSpawnPoint.position, noteSpawnPoint.rotation);
        note.ServerSetCandlesticksPuzzleText();
    }
}
