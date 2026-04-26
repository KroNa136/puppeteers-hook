using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SanityManager : NetworkBehaviour
{
    [SerializeField] private PlayerAudioController _audioController;

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

    [Space]

    [SerializeField] private float _noToLowSanityLossBoundary = 0.75f;
    [SerializeField] private float _lowToMediumSanityLossBoundary = 0.5f;
    [SerializeField] private float _mediumToHighSanityLossBoundary = 0.25f;
    [SerializeField] private float _highToMaximumSanityLossBoundary = 0.01f;

    public float CurrentSanityFraction => CurrentSanity / _maxSanity;
    public bool LowSanityLoss => CurrentSanityFraction <= _noToLowSanityLossBoundary;
    public bool MediumSanityLoss => CurrentSanityFraction <= _lowToMediumSanityLossBoundary;
    public bool HighSanityLoss => CurrentSanityFraction <= _mediumToHighSanityLossBoundary;
    public bool MaximumSanityLoss => CurrentSanityFraction <= _highToMaximumSanityLossBoundary;

    private float _tickRate = 1f / 30f;

    private Collider[] _overlaps;

    private DoorAudioController[] _doorAudioControllers;
    private WardrobeDoorAudioController[] _wardrobeDoorAudioControllers;

    [Space]

    [SerializeField][Min(0.01f)] private float _amuletAnimationInterval = 5f;
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
        _wardrobeDoorAudioControllers = FindObjectsByType<WardrobeDoorAudioController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        _amuletAnimationTimer = 0f;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

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

        if (overlapCount > 0)
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

        _ = _gameHud.Bind
        (
            (hud, sanity, maxSanity) => hud.SetInvestigatorSanity(sanity, maxSanity),
            newValue, _maxSanity
        );

        // TODO: remake sanity levels
        //       current fraction => visual indicator, audio indicator, note readability
        //       low (> 0.5) => creaks, thumps, steps
        //       high (<= 0.5) => whispers, voices

        _ = _audioController
            .Bind((c, lowSanityLoss) => c.SetLowSanityLossSounds(lowSanityLoss), LowSanityLoss)
            .Bind((c, mediumSanityLoss) => c.SetMediumSanityLossSounds(mediumSanityLoss), MediumSanityLoss)
            .Bind((c, sanityLossVolume) => c.SetSanityDecreaseAreaVolume(sanityLossVolume), 1 - (newValue / _maxSanity));

        if (_doorAudioControllers != null)
        {
            foreach (var controller in _doorAudioControllers)
                controller.SetHighSanityLossSounds(HighSanityLoss);
        }

        if (_wardrobeDoorAudioControllers != null)
        {
            foreach (var controller in _wardrobeDoorAudioControllers)
                controller.SetHighSanityLossSounds(HighSanityLoss);
        }
    }

    /*
    [Header("References")]

    [SerializeField] Image sanityIndicator;
    [SerializeField] PlayerAudioController playerAudioController;

    [Header("Values")]

    public bool enableRegeneration = true;
    public float sanity = 100f;
    [SerializeField] float maxSanity = 100f;
    [SerializeField] bool enableSanityLossEffects = true;

    [Header("Speeds")]

    [SerializeField] float regenerationSpeed = 10f; // 10 seconds for 0-100
    [SerializeField] float decreaseSpeed = 0.333f; // 300 seconds (5 minutes) for 100-0
    [SerializeField] float decreaseToTargetSpeed = 3.333f; // 30-ish seconds for 100-0

    [Header("Separation")]

    [SerializeField] float lowSanityLossFraction = 0.75f;
    [SerializeField] float mediumSanityLossFraction = 0.5f;
    [SerializeField] float highSanityLossFraction = 0.25f;
    [SerializeField] float maximumSanityLossFraction = 0.01f;

    bool isDecreasingToTarget = false;
    bool isDecreasing = false;

    float targetSanity;

    float indicatorValue;

    DoorAudioController[] doorAudioControllers;
    WardrobeDoorAudioController[] wardrobeDoorAudioControllers;
    WallText[] wallTexts;
    Hallucination[] hallucinations;

    int i;
    
    void Start()
    {
        sanity = maxSanity;

        GetDoorAudioControllers();
        GetWardrobeDoorAudioControllers();
        GetWallTexts();
        GetHallucinations();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            DecreaseTest();

        if (isDecreasingToTarget || isDecreasing)
        {
            if (isDecreasingToTarget)
            {
                sanity = Mathf.Lerp(sanity, targetSanity, decreaseToTargetSpeed * Time.deltaTime);

                if (sanity - 0.1f < targetSanity)
                {
                    sanity = targetSanity;
                    isDecreasingToTarget = false;
                }
            }
            
            if (isDecreasing)
            {
                sanity -= decreaseSpeed * Time.deltaTime;

                if (sanity < 0f)
                {
                    sanity = 0f;
                    StopDecreasing();
                }
            }
        }
        else if (enableRegeneration)
        {
            if (sanity < maxSanity)
                sanity += regenerationSpeed * Time.deltaTime;
            else if (sanity > maxSanity)
                sanity = maxSanity;
        }

        if (enableSanityLossEffects)
        {
            playerAudioController.SetLowSanityLossSounds(sanity / maxSanity <= lowSanityLossFraction);
            playerAudioController.SetMediumSanityLossSounds(sanity / maxSanity <= mediumSanityLossFraction);

            for (i = 0; i < doorAudioControllers.Length; i++)
                doorAudioControllers[i].SetHighSanityLossSounds(sanity / maxSanity <= highSanityLossFraction);
            
            for (i = 0; i < wardrobeDoorAudioControllers.Length; i++)
                wardrobeDoorAudioControllers[i].SetHighSanityLossSounds(sanity / maxSanity <= highSanityLossFraction);
            
            for (i = 0; i < wallTexts.Length; i++)
                wallTexts[i].SetVisible(sanity / maxSanity <= highSanityLossFraction);

            for (i = 0; i < hallucinations.Length; i++)
                hallucinations[i].SetBehaviour(sanity / maxSanity <= maximumSanityLossFraction);
        }

        indicatorValue = 1f - sanity / maxSanity;

        sanityIndicator.color = new Color32((byte)255, (byte)255, (byte)255, (byte)(indicatorValue * 255));
        playerAudioController.SetSanityDecreaseAreaVolume(indicatorValue);
    }

    public void StartDecreasing()
    {
        isDecreasing = true;
    }

    public void StopDecreasing()
    {
        isDecreasing = false;
    }

    public void DecreaseBy(float amount)
    {
        targetSanity = sanity - amount;

        if (targetSanity < 0f)
            targetSanity = 0f;

        isDecreasingToTarget = true;
    }

    [ContextMenu("Decrease by 10")]
    void DecreaseTest()
    {
        DecreaseBy(10f);
    }

    public void GetDoorAudioControllers()
    {
        doorAudioControllers = FindObjectsByType<DoorAudioController>(FindObjectsSortMode.None);
    }

    public void GetWardrobeDoorAudioControllers()
    {
        wardrobeDoorAudioControllers = FindObjectsByType<WardrobeDoorAudioController>(FindObjectsSortMode.None);
    }

    public void GetWallTexts()
    {
        wallTexts = FindObjectsByType<WallText>(FindObjectsSortMode.None);
    }

    public void GetHallucinations()
    {
        hallucinations = FindObjectsByType<Hallucination>(FindObjectsSortMode.None);
    }
    */
}
