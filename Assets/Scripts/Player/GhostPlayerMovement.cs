using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(GhostStaminaManager))]
public class GhostPlayerMovement : GamePlayerMovement
{
    [SyncVar]
    public bool CanDash;

    [SyncVar(hook = nameof(OnClientIsPreparingToDashChanged))]
    public bool IsPreparingToDash;

    private bool _predictedIsPreparingToDash;

    [SyncVar(hook = nameof(OnClientIsDashingChanged))]
    public bool IsDashing;

    [SyncVar]
    public Vector3 DashDirection;

    [SyncVar]
    public bool IsInPreparePhase;

    // [SerializeField] private GhostAudioController _ghostAudioController;

    [Space]

    [SerializeField] private float _walkingSpeed = 0.75f;
    [SerializeField] private float _dashSpeed = 6f;
    [SerializeField] private float _preparePhaseSpeed = 3f;

    [Space]

    [SerializeField] private float _dashChargeDuration = 1f;
    [SerializeField] private float _dashPrepareDuration = 1f;

    private float _dashChargeTime = 0f;
    private bool _sentDashCommand = false;

    [Space]

    [SerializeField] private int _fearGameTimeDecrease = 10;
    [SerializeField] private float _touchedInvestigatorsCheckControllerRadiusInflation = 3f;
    private Collider[] _playerOverlaps;
    private bool _inducedFear = false;

    [Space]

    [SerializeField] private LayerMask _roomLayerMask;
    private Collider[] _roomOverlaps;

    private GhostStaminaManager _staminaManager;
    private GhostVisibilityManager _visibilityManager;
    private GameHud _gameHud;

    protected override void OnStart()
    {
        _staminaManager = GetComponent<GhostStaminaManager>();
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        base.OnStartServer();

        _ = TryGetComponent(out _visibilityManager);

        CanDash = true;
        IsDashing = false;
        IsInPreparePhase = false;

        _playerOverlaps = new Collider[20];
        _roomOverlaps = new Collider[20];
    }

    [Client]
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        base.OnStartAuthority();

        _gameHud = FindAnyObjectByType<GameHud>();
        _ = _gameHud.Bind((hud, maxStamina) => hud.InitializeGhostDashChargeBar(maxStamina), _dashChargeDuration);
    }

    protected override void OnUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (_inputManager.Sprint && _staminaManager.GetOrDefault(sm => sm.IsMaxStamina) && !_predictedIsPreparingToDash && !IsPreparingToDash && !IsDashing && CanDash && !_sentDashCommand)
        {
            _dashChargeTime += Time.deltaTime;
            _ = _gameHud.Bind((hud, chargeTime) => hud.SetGhostDashCharge(chargeTime), _dashChargeTime);

            if (_dashChargeTime >= _dashChargeDuration)
            {
                CmdDash();
                _sentDashCommand = true;
                _predictedIsPreparingToDash = true;
                //_ = _audioController.Bind(c => c.PlayDashPrepareSound());
            }
        }
        else
        {
            _dashChargeTime = 0f;
            _ = _gameHud.Bind(hud => hud.SetGhostDashCharge(0f));
        }
    }

    [Client]
    public void OnClientIsPreparingToDashChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;

        //if (newValue && !_predictedIsPreparingToDash)
        //    _ = _audioController.Bind(c => c.PlayDashPrepareSound());

        _predictedIsPreparingToDash = newValue;
    }

    [Client]
    public void OnClientIsDashingChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;

        //if (newValue)
        //    _ = _audioController.Bind(c => c.PlayDashSound());

        var layer = newValue ? LayerMask.NameToLayer("Ghost Dashing") : LayerMask.NameToLayer("Ghost");

        gameObject.layer = layer;
        foreach (Transform child in transform)
            child.gameObject.layer = layer;
    }

    [Command]
    public void CmdDash()
    {
        if (_staminaManager.GetOrDefault(sm => !sm.IsMaxStamina) || IsPreparingToDash || IsDashing || !CanDash)
            return;

        IsPreparingToDash = true;
        CanDash = false;

        _ = StartCoroutine(Dash());

        TargetRpcResetDashCommand(connectionToClient);
    }

    [Server]
    public IEnumerator Dash()
    {
        if (!isServer)
            yield break;

        var dashingLayer = LayerMask.NameToLayer("Ghost Dashing");
        var normalLayer = LayerMask.NameToLayer("Ghost");

        if (TryGetComponent(out GhostStealthAbility stealthAbility) && stealthAbility.IsActivated)
            stealthAbility.ServerDeactivate();

        _ = _visibilityManager.Bind(vm => vm.ServerSetHighVisibility());

        IsPreparingToDash = true;

        yield return new WaitForSeconds(_dashPrepareDuration);

        DashDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
        IsPreparingToDash = false;
        IsDashing = true;

        gameObject.layer = dashingLayer;
        foreach (Transform child in transform)
            child.gameObject.layer = dashingLayer;

        while (_staminaManager.GetOrDefault(sm => sm.CurrentStamina) > 0f)
            yield return null;

        DashDirection = Vector3.zero;
        IsDashing = false;

        _inducedFear = false;

        gameObject.layer = normalLayer;
        foreach (Transform child in transform)
            child.gameObject.layer = normalLayer;

        _ = _visibilityManager.Bind(vm => vm.ServerSetLowVisibility());

        while (_staminaManager.GetOrDefault(sm => !sm.IsMaxStamina))
            yield return null;

        CanDash = true;
    }

    [TargetRpc]
    public void TargetRpcResetDashCommand(NetworkConnectionToClient conn)
    {
        _sentDashCommand = false;
    }

    [Server]
    protected override void AfterServerSimulate(PlayerInputData input, float deltaTime)
    {
        if (!isServer)
            return;

        if (IsDashing)
        {
            _ = _staminaManager.Bind((staminaManager, dt) => staminaManager.ServerDrain(dt), deltaTime);

            if (_inducedFear)
                return;

            var touchedFearReceivers = ServerGetTouchedFearReceivers();

            if (touchedFearReceivers.None())
                return;

            _inducedFear = true;
            GameManager.Instance.ServerDecreaseGameTimeBy(_fearGameTimeDecrease);
            RpcInduceFear();

            foreach (var fearReceiver in touchedFearReceivers)
                fearReceiver.ServerStartBeingAfraidOf(transform);

            if (!TryGetComponent(out GhostIllusionAbility ghostIllusionAbility) || !ghostIllusionAbility.IsActivated)
                return;

            var roomsWithIllusion = ghostIllusionAbility.RoomsWithIllusion;

            var roomsFearReceiversAreIn = touchedFearReceivers.SelectMany(fearReceiver =>
            {
                int overlapCount = Physics.OverlapSphereNonAlloc
                (
                    position: fearReceiver.transform.position,
                    radius: 0.01f,
                    results: _roomOverlaps,
                    layerMask: _roomLayerMask,
                    queryTriggerInteraction: QueryTriggerInteraction.Collide
                );

                return _roomOverlaps
                    .Take(overlapCount)
                    .Select(o => o.TryGetComponent(out Room room) ? room : null)
                    .NonNullItems();
            });

            bool atLeastOneReceiverIsInRoomWithIllusion = roomsWithIllusion.Intersect(roomsFearReceiversAreIn).Any();

            if (atLeastOneReceiverIsInRoomWithIllusion)
                ghostIllusionAbility.ServerDeactivate();
        }
        else
        {
            _ = _staminaManager.Bind((staminaManager, dt, isMoving) => staminaManager.ServerRegenerate(dt, isMoving), deltaTime, input.Move.sqrMagnitude > 0.001f);
        }
    }

    [Server]
    private IEnumerable<FearManager> ServerGetTouchedFearReceivers()
    {
        if (!isServer)
            return CreateList.With<FearManager>();

        Vector3 point0 = new(transform.position.x, TopY - _controller.radius, transform.position.z);
        Vector3 point1 = new(transform.position.x, BottomY + _controller.radius, transform.position.z);
        float inflatedRadius = _controller.radius + _touchedInvestigatorsCheckControllerRadiusInflation;

        int overlapCount = Physics.OverlapCapsuleNonAlloc
        (
            point0,
            point1, 
            radius: inflatedRadius,
            results: _playerOverlaps
        );

        var touchedFearReceivers = _playerOverlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out FearManager fearManager) ? fearManager : null)
            .NonNullItems()
            .ToList();

        return touchedFearReceivers;
    }

    [ClientRpc]
    public void RpcInduceFear()
    {
        // _ = _audioController.Bind(c => c.PlayInduceFearSound());
    }

    protected override void MoveVertically(PlayerInputData input, float deltaTime)
    {
        _verticalSpeed = _isGrounded
            ? _defaultVerticalForce
            : Mathf.Clamp
                (
                    value: _verticalSpeed + Physics.gravity.y * deltaTime,
                    min: _terminalFallingVelocity,
                    max: float.PositiveInfinity
                );

        _ = _controller.Move(_verticalSpeed * deltaTime * Vector3.up);
    }

    protected override void Move(PlayerInputData input, float deltaTime)
    {
        if (_predictedIsPreparingToDash || IsPreparingToDash)
            return;

        float speed = IsInPreparePhase ? _preparePhaseSpeed : IsDashing ? _dashSpeed : _walkingSpeed;

        Vector3 clampedMove = Vector3.ClampMagnitude
        (
            vector: IsDashing
                ? DashDirection
                : transform.right * input.Move.x + transform.forward * input.Move.y,
            maxLength: 1f
        );

        _ = _controller.Move(speed * deltaTime * clampedMove);
    }

    protected override Vector2 CalculateDesiredMove(Vector3 startPosition, Vector3 endPosition, float deltaTime)
    {
        if (_predictedIsPreparingToDash || IsPreparingToDash || IsDashing)
            return Vector2.zero;

        Vector3 startPositionWithoutY = new(startPosition.x, 0f, startPosition.z);
        Vector3 endPositionWithoutY = new(endPosition.x, 0f, endPosition.z);

        Vector3 positionDelta = endPositionWithoutY - startPositionWithoutY;

        float forwardProjection = Vector3.Dot(positionDelta, transform.forward);
        float rightProjection = Vector3.Dot(positionDelta, transform.right);

        return new Vector2(rightProjection, forwardProjection) / (_walkingSpeed * deltaTime);
    }
}
