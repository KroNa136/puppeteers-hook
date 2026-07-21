using UnityEngine;

public class HoldablePlacementTarget : MonoBehaviour
{
    public HoldableType HoldableType { get; set; } = HoldableType.None;

    [SerializeField] private string _displayName;
    public string DisplayName => _displayName;
}
