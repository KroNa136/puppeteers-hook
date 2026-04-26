using Mirror;
using UnityEngine;

public class InvestigatorStaminaManager : StaminaManager
{
    [SerializeField] private PlayerAudioController _audioController;

    private GameHud _gameHud;

    [Space]

    [SerializeField] private float _criticalStaminaFraction = 0.2f;
    [SerializeField] private float _sprintTime = 20f;

    public bool IsCriticalStamina => CurrentStamina / _maxStamina <= _criticalStaminaFraction;

    [Client]
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        base.OnStartAuthority();

        _gameHud = FindAnyObjectByType<GameHud>();
        _ = _gameHud.Bind((hud, maxStamina) => hud.InitializeInvestigatorStaminaBar(maxStamina), _maxStamina);
    }

    [Server]
    public override void ServerDrain(float deltaTime)
    {
        if (!isServer)
            return;

        CurrentStamina = Mathf.Clamp
        (
            value: CurrentStamina - _maxStamina * (deltaTime / _sprintTime),
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
            (hud, stamina, maxStamina, isCriticalStamina) => hud.SetInvestigatorStamina(stamina, maxStamina, isCriticalStamina),
            newValue, _maxStamina, IsCriticalStamina
        );

        // 1 - stamina/maxStamina = [0; 1] * (1 - (1 - critFraction)) + (1 - critFraction)
        // [0; 1] = (1 - stamina/maxStamina - (1 - critFraction)) / (1 - (1 - critFraction))
        float dyspneaVolume = (_criticalStaminaFraction - (newValue / _maxStamina)) / _criticalStaminaFraction;
        _ = _audioController.Bind((controller, volume) => controller.SetDyspneaSoundVolume(volume), dyspneaVolume);
    }
}
