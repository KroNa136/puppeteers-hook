using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private int _id = 0;
    public int Id => _id;

    [HideInInspector] public NetworkRoom LinkedNetworkRoom;

    [SerializeField] private RoomType _type = RoomType.None;
    public RoomType Type => _type;

    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private List<Transform> _doorways;
    [SerializeField] private List<Transform> _neighborDetectors;

    public Vector3 PlayerSpawnPosition => _playerSpawnPoint.TryGet(sp => sp.position, out var position) ? position : transform.position;
    public List<Transform> Doorways => _doorways.ToList();
    public List<Transform> NeighborDetectors => _neighborDetectors.ToList();

    [Space]

    [SerializeField] private LayerMask _doorLayerMask;
    [SerializeField] private LayerMask _roomLayerMask;

    public LayerMask DoorLayerMask => _doorLayerMask;
    public LayerMask RoomLayerMask => _roomLayerMask;

    [Space]

    [SerializeField] private List<GameObject> _illusionSets;
    [SerializeField] private List<GameObject> _illusionParticleSystemSets;

    public List<GameObject> IllusionSets => _illusionSets.ToList();
    public List<GameObject> IllusionParticleSystemSets => _illusionParticleSystemSets.ToList();
}
