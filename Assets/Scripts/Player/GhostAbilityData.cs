using UnityEngine;

[CreateAssetMenu(menuName = "Ghost Abilities/Ghost Ability Data")]
public class GhostAbilityData : ScriptableObject
{
    [SerializeField] private string _name;
    public string Name => _name;

    [SerializeField] private string _description;
    public string Description => _description;
    
    [SerializeField] private Sprite _icon;
    public Sprite Icon => _icon;
    
    [SerializeField] private int _duration;
    public int Duration => _duration;

    [SerializeField] private int _cooldown;
    public int Cooldown => _cooldown;
}
