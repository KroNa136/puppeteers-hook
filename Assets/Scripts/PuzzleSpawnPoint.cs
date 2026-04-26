using UnityEngine;

public class PuzzleSpawnPoint : SpawnPoint
{
    [SerializeField] private PuzzleType _puzzleType = PuzzleType.None;
    public PuzzleType PuzzleType => _puzzleType;
}
