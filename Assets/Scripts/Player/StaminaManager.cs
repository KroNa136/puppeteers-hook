using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public abstract class StaminaManager : NetworkBehaviour
{
    [SerializeField] protected float _maxStamina = 100f;
    [SerializeField] protected float _regenerationTimeIfStanding = 20f;
    [SerializeField] protected float _regenerationTimeIfMoving = 40f;

    [SyncVar(hook = nameof(OnClientStaminaChanged))]
    public float CurrentStamina;

    public override void OnStartServer()
    {
        if (!isServer)
            return;

        CurrentStamina = _maxStamina;
    }

    [Server]
    public void ServerRegenerate(float deltaTime, bool isMoving)
    {
        if (!isServer)
            return;

        float regenerationTime = isMoving ? _regenerationTimeIfMoving : _regenerationTimeIfStanding;

        CurrentStamina = Mathf.Clamp
        (
            value: CurrentStamina + _maxStamina * (deltaTime / regenerationTime),
            min: 0f,
            max: _maxStamina
        );
    }

    public abstract void ServerDrain(float deltaTime);
    public abstract void OnClientStaminaChanged(float oldValue, float newValue);
}
