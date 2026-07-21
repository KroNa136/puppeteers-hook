using Mirror;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField] private Transform _handObject;
    public Transform HandObject => _handObject;
}
