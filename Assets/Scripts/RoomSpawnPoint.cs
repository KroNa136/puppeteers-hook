using UnityEngine;

public class RoomSpawnPoint : SpawnPoint
{
    [SerializeField] private RoomType _roomType = RoomType.None;
    public RoomType RoomType => _roomType;
}
