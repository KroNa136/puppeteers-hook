using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FlashlightManager : NetworkBehaviour
{
    [SerializeField] private Light _light;
    [SerializeField] private PlayerAudioController _audioController;

    private InputManager _inputManager;
    private GameHud _gameHud;

    [Space]

    [SerializeField] private float _maxCharge = 100f;
    [SerializeField] private float _dischargeTime = 30f;
    [SerializeField] private float _rechargeTime = 30f;
    [SerializeField] private float _criticalChargeFraction = 0.2f;

    [SyncVar(hook = nameof(OnClientEnabledChanged))]
    public bool IsEnabled;

    private bool _predictedIsEnabled;
    private bool PredictedIsEnabled
    {
        get => _predictedIsEnabled;
        set
        {
            if (_predictedIsEnabled != value)
                _ = _audioController.Bind(c => c.PlayFlashlightSound());

            _predictedIsEnabled = value;
            _ = _light.Bind(l => l.enabled = value);
        }
    }

    [SyncVar(hook = nameof(OnClientChargeChanged))]
    public float CurrentCharge;

    public bool IsCriticalCharge => CurrentCharge / _maxCharge <= _criticalChargeFraction;

    private float _tickRate = 1f / 30f;

    public bool CanBeControlledByPlayer = true;

    private void Start()
    {
        _ = _light.Bind(l => l.enabled = false);

        GameManager.OnClientGameOver.AddListener(OnGameOver);
    }

    private void OnGameOver(bool _)
    {
        CanBeControlledByPlayer = false;
    }

    public override void OnStartServer()
    {
        if (!isServer)
            return;

        IsEnabled = false;
        CurrentCharge = _maxCharge;

        var tickSystem = TickSystem.Instance;
        _tickRate = tickSystem.TickRate;
        tickSystem.OnTick.AddListener(ServerTick);
    }

    [Client]
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        _gameHud = FindAnyObjectByType<GameHud>();
        _ = _gameHud.Bind((hud, maxCharge) => hud.InitializeInvestigatorFlashlightChargeBar(maxCharge), _maxCharge);

        if (!TryGetComponent(out _inputManager))
            _ = StartCoroutine(WaitForInputManager());
    }

    [Client]
    public IEnumerator WaitForInputManager()
    {
        while (!TryGetComponent(out _inputManager))
            yield return new WaitForSeconds(0.5f);
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (!CanBeControlledByPlayer)
            return;

        bool toggled = _inputManager.GetOrDefault(im => im.Flashlight);

        if (toggled)
        {
            PredictedIsEnabled = !PredictedIsEnabled;
            CmdToggle();
        }
    }

    [Command]
    public void CmdToggle()
    {
        IsEnabled = !IsEnabled;
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        if (IsEnabled)
            ServerDischarge(_tickRate);
        else if (CurrentCharge < _maxCharge)
            ServerRecharge(_tickRate);

        if (CurrentCharge == 0f)
            IsEnabled = false;
    }

    [Server]
    public void ServerDischarge(float deltaTime)
    {
        if (!isServer)
            return;

        CurrentCharge = Mathf.Clamp
        (
            value: CurrentCharge - _maxCharge * (deltaTime / _dischargeTime),
            min: 0f,
            max: _maxCharge
        );
    }

    [Server]
    public void ServerRecharge(float deltaTime)
    {
        if (!isServer)
            return;

        CurrentCharge = Mathf.Clamp
        (
            value: CurrentCharge + _maxCharge * (deltaTime / _rechargeTime),
            min: 0f,
            max: _maxCharge
        );
    }

    [Client]
    public void OnClientEnabledChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;

        PredictedIsEnabled = newValue;
    }

    [Client]
    public void OnClientChargeChanged(float oldValue, float newValue)
    {
        if (!isClient)
            return;

        if (newValue == 0f)
            PredictedIsEnabled = false;

        if (!isLocalPlayer)
            return;

        _ = _gameHud.Bind
        (
            (hud, charge, maxCharge, isCriticalCharge) => hud.SetInvestigatorFlashlightCharge(charge, maxCharge, isCriticalCharge),
            newValue, _maxCharge, IsCriticalCharge
        );
    }

    /*
    [Header("References")]

    [SerializeField] Light lightComponent;
    [SerializeField] GameObject chargeBar;
    [SerializeField] Image chargeBarIcon;
    [SerializeField] Image chargeBarFill;
    [SerializeField] Slider slider;
    [SerializeField] PlayerAudioController audioController;

    [Header("Values")]

    [SerializeField] float charge = 60f;
    [SerializeField] float maxCharge = 60f;
    [SerializeField] float criticalChargeFraction = 0.2f;

    [Header("Speeds")]

    [SerializeField] float dischargeSpeed = 1f;
    [SerializeField] float rechargeSpeed = 1f;

    Color color;

    void Start()
    {
        slider.minValue = 0f;
        slider.maxValue = maxCharge;
        slider.value = maxCharge;

        charge = maxCharge;
    }

    void Update()
    {
        if (Input.GetButtonDown("Flashlight"))
            ToggleLight();

        if (lightComponent.enabled)
        {
            charge -= dischargeSpeed * Time.deltaTime;

            if (charge <= 0)
                ToggleLight();
        }
        else
        {
            if (charge < maxCharge)
                charge += rechargeSpeed * Time.deltaTime;
            else if (charge > maxCharge)
                charge = maxCharge;
        }

        slider.value = charge;

        if (charge / maxCharge < criticalChargeFraction)
            color = new Color(1f, 0f, 0f, 0.5f);
        else
            color = new Color(1f, 1f, 1f, 0.5f);

        chargeBarIcon.color = color;
        chargeBarFill.color = color;

        chargeBar.SetActive(charge < maxCharge);
    }

    void ToggleLight()
    {
        audioController.PlayFlashlightSound();

        lightComponent.enabled = !lightComponent.enabled;
    }

    public void TurnOff()
    {
        audioController.PlayFlashlightSound();

        lightComponent.enabled = false;
    }
    */
}
