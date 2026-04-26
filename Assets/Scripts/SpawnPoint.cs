using Mirror;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private SpawnPointType _type = SpawnPointType.Empty;
    public SpawnPointType Type => _type;

    [SerializeField][Range(0f, 1f)] private float _spawnChance = 0.5f;
    public float SpawnChance => _spawnChance;
}
