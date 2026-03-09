using UnityEngine;

[System.Flags]
public enum PlayerRole
{
    None = 0,
    Ghost = 1 << 0,
    Investigator = 1 << 1
}
