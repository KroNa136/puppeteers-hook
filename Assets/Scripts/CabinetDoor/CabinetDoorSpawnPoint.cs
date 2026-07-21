using UnityEngine;

public class CabinetDoorSpawnPoint : SpawnPoint
{
    [SerializeField] private bool _isLeftDoor;
    public bool IsLeftDoor => _isLeftDoor;
}
