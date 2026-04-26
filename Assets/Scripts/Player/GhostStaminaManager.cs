using Mirror;
using UnityEngine;

public class GhostStaminaManager : StaminaManager
{
    [SerializeField] private float _dashTime = 1f;

    private GameHud _gameHud;

    public bool IsMaxStamina => CurrentStamina == _maxStamina;

    [Client]
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        base.OnStartAuthority();

        _gameHud = FindAnyObjectByType<GameHud>();
        _ = _gameHud.Bind((hud, maxStamina) => hud.InitializeGhostStaminaBar(maxStamina), _maxStamina);
    }

    [Server]
    public override void ServerDrain(float deltaTime)
    {
        if (!isServer)
            return;

        CurrentStamina = Mathf.Clamp
        (
            value: CurrentStamina - _maxStamina * (deltaTime / _dashTime),
            min: 0f,
            max: _maxStamina
        );
    }

    [Client]
    public override void OnClientStaminaChanged(float oldValue, float newValue)
    {
        if (!isLocalPlayer)
            return;

        _ = _gameHud.Bind
        (
            (hud, stamina, maxStamina) => hud.SetGhostStamina(stamina, maxStamina),
            newValue, _maxStamina
        );
    }
}
