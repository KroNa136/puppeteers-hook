using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SanityManager : NetworkBehaviour
{
    [SerializeField] private InvestigatorAudioController _audioController;

    private CharacterController _controller;
    private GameHud _gameHud;

    [SerializeField] private LayerMask _sanityLossAreaLayerMask;

    [Space]

    [SerializeField] private float _maxSanity = 100f;
    [SerializeField] private float _depletionTimeIfDoesNotHaveAmulet = 60f;
    [SerializeField] private float _depletionTimeIfHasAmulet = 120f;
    [SerializeField] private float _regenerationTime = 60f;

    [SyncVar(hook = nameof(OnClientCurrentSanityChanged))]
    public float CurrentSanity;

    [SyncVar]
    public bool IsInSanityLossArea;

    [Space]

    [SerializeField] private float _lowToHighSanityLossBoundary = 0.5f;

    public float CurrentSanityFraction => CurrentSanity / _maxSanity;
    public bool LowSanityLoss => CurrentSanityFraction < 1f;
    public bool HighSanityLoss => CurrentSanityFraction <= _lowToHighSanityLossBoundary;

    private float _tickRate = 1f / 30f;

    private Collider[] _overlaps;

    private DoorAudioController[] _doorAudioControllers;

    [Space]

    [SerializeField][Min(0.01f)] private float _amuletAnimationInterval = 2f;
    private float _amuletAnimationTimer;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _ = TryGetComponent(out _controller);

        _overlaps = new Collider[20];

        CurrentSanity = _maxSanity;

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
        _ = _gameHud.Bind((hud, maxSanity) => hud.SetInvestigatorSanity(maxSanity, maxSanity), _maxSanity);

        _doorAudioControllers = FindObjectsByType<DoorAudioController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        _amuletAnimationTimer = 0f;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (!IsInSanityLossArea)
        {
            _amuletAnimationTimer = 0f;
            return;
        }

        _amuletAnimationTimer += Time.deltaTime;

        if (_amuletAnimationTimer >= _amuletAnimationInterval)
        {
            _ = _gameHud.Bind(hud => hud.ShakeInvestigatorAmuletIcon());
            _amuletAnimationTimer = 0f;
        }
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: transform.position,
            radius: _controller.TryGet(c => c.radius, out var radius) ? radius : 1f,
            results: _overlaps,
            layerMask: _sanityLossAreaLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Collide
        );

        IsInSanityLossArea = overlapCount > 0;

        if (IsInSanityLossArea)
            ServerDeplete(_tickRate);
        else
            ServerRegenerate(_tickRate);
    }

    [Server]
    public void ServerDeplete(float deltaTime)
    {
        if (!isServer)
            return;

        float depletionTime = TryGetComponent(out Inventory inventory) && inventory.HasAmulet
            ? _depletionTimeIfHasAmulet
            : _depletionTimeIfDoesNotHaveAmulet;

        CurrentSanity = Mathf.Clamp
        (
            value: CurrentSanity - _maxSanity * (deltaTime / depletionTime),
            min: 0f,
            max: _maxSanity
        );
    }

    [Server]
    public void ServerRegenerate(float deltaTime)
    {
        if (!isServer)
            return;

        CurrentSanity = Mathf.Clamp
        (
            value: CurrentSanity + _maxSanity * (deltaTime / _regenerationTime),
            min: 0f,
            max: _maxSanity
        );
    }

    [Client]
    public void OnClientCurrentSanityChanged(float oldValue, float newValue)
    {
        if (!isLocalPlayer)
            return;

        if (newValue > oldValue || newValue == _maxSanity)
            _amuletAnimationTimer = 0f;

        // Sanity effects:
        // - current fraction => visual indicator (red vignette), audio indicator, note readability
        // - low (> 0.5) => creaks, thumps, steps
        // - high (<= 0.5) => whispers, voices

        _ = _gameHud.Bind
        (
            (hud, sanity, maxSanity) => hud.SetInvestigatorSanity(sanity, maxSanity),
            newValue, _maxSanity
        );

        _ = _audioController
            .Bind((c, lowSanityLoss) => c.LowSanityLoss = lowSanityLoss, LowSanityLoss)
            .Bind((c, highSanityLoss) => c.HighSanityLoss = highSanityLoss, HighSanityLoss)
            .Bind((c, sanityLossVolume) => c.SetSanityDecreaseVolume(sanityLossVolume), 1f - (newValue / _maxSanity));

        if (_doorAudioControllers != null)
        {
            foreach (var controller in _doorAudioControllers)
                controller.PlayerHasSanityLoss = LowSanityLoss;
        }
    }
}
