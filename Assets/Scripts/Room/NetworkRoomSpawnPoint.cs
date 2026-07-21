using UnityEngine;

public class NetworkRoomSpawnPoint : SpawnPoint
{
    [SerializeField] private Room _room;
    public Room Room => _room;
}
