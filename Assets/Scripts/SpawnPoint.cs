using Mirror;
using UnityEngine;

public class SpawnPoint : NetworkBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField][Range(0f, 1f)] private float _spawnChance = 0.5f;
    [SerializeField] private int _seed = 1;

    [Server]
    public void ServerSpawn()
    {
        if (!isServer)
            return;

        if (_spawnChance == 0f)
            return;

        Random.State savedState = Random.state;
        Random.InitState(_seed);

        bool spawn = Random.value <= _spawnChance;

        Random.state = savedState;

        if (!spawn)
            return;

        var gameObj = Instantiate(_prefab, transform.position, Quaternion.identity, transform);
        NetworkServer.Spawn(gameObj);
    }
}
