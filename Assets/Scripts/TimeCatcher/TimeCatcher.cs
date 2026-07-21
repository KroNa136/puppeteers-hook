using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class TimeCatcher : NetworkBehaviour
{
    [SerializeField] private TimeCatcherAudioController _audioController;

    private CharacterController _controller;
    protected NetworkAnimator _animator;

    [Space]

    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 270f;

    [Space]

    [SerializeField] private ParticleSystem _onAttackParticleSystem;
    [SerializeField] private float _touchedTargetsCheckControllerRadiusInflation = 0.4f;
    [SerializeField] private int _gameTimeDecrease = 10;
    [SerializeField] private float _destroyDelay = 3.5f;
    [SerializeField] private float _becomeInvisibleDelay = 1.5f;

    [SerializeField] private LayerMask _roomLayerMask;
    [SerializeField] private LayerMask _investigatorLayerMask;
    [SerializeField] private LayerMask _clockLayerMask;

    private Collider[] _selfRoomOverlaps;
    private Collider[] _investigatorRoomOverlaps;
    private Collider[] _clockOverlaps;

    [Space]

    [SerializeField] private float _groundDistanceOffset = 0.05f;
    [SerializeField] private LayerMask _groundMask;

    private bool _isGrounded;
    private Collider[] _groundOverlaps;

    [Space]

    [SerializeField] private float _terminalFallingVelocity = -50f;
    [SerializeField] private float _defaultVerticalForce = -1.5f;

    public float TopY => transform.position.y + _controller.center.y + (_controller.height * 0.5f);
    public float BottomY => transform.position.y + _controller.center.y - (_controller.height * 0.5f);

    private float _verticalSpeed;

    private float _tickRate = 1f / 30f;

    private Vector3 _previousSimulatedPosition;
    private Vector3 _currentSimulatedPosition;
    private float _motionInterpolationTimer;

    [Space]

    [SyncVar]
    public bool NeutralMode;

    [SerializeField] private float _clockVisionRadius = 2f;

    [SyncVar(hook = nameof(OnClientHasAttackedChanged))]
    public bool HasAttacked;

    private Collider[] _clockOverlaps2;
    private Collider[] _playerOverlaps;
    private bool _isWaitingForInvestigatorsToLeaveTriggerAfterSpawn = false;

    [Space]

    [SerializeField] private float _waitingTimeBeforeGoingToSpawnPosition = 5f;
    private Vector3 _spawnPosition;
    private float _waitingBeforeGoingToSpawnPositionTimer = 0f;

    [Space]
    [SerializeField] private float _footstepInterval = 0.8f;
    private float _footstepTimer;

    [Client]
    public void OnClientHasAttackedChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;

        if (oldValue == newValue)
            return;

        if (!newValue)
            return;

        _ = _onAttackParticleSystem.Bind(ps => ps.Play());
        _ = _audioController.Bind(c => c.PlayAttackSound());

        if (!NeutralMode)
            _ = StartCoroutine(MakeInvisibleAfterDelay(_becomeInvisibleDelay));
    }

    [Client]
    public IEnumerator MakeInvisibleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        var renderers = GetComponentsInChildren<Renderer>();

        if (TryGetComponent(out Renderer renderer))
            renderers = renderers.Prepend(renderer).ToArray();

        foreach (var rend in renderers)
            rend.enabled = false;
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        _previousSimulatedPosition = transform.position;
        _currentSimulatedPosition = transform.position;
        _motionInterpolationTimer = 0f;
    }

    private void Update()
    {
        if (isClient)
            ClientInterpolateMovement();
    }

    [Client]
    public void ClientInterpolateMovement()
    {
        if (!isClient)
            return;

        _motionInterpolationTimer += Time.deltaTime;

        float t = Mathf.Clamp01(_motionInterpolationTimer / _tickRate);
        var targetPosition = Vector3.Lerp(_previousSimulatedPosition, _currentSimulatedPosition, t);

        TeleportTo(targetPosition);
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _controller = GetComponent<CharacterController>();

        _ = TryGetComponent(out _animator);

        NeutralMode = false;
        HasAttacked = false;

        _isGrounded = false;
        _groundOverlaps = new Collider[20];

        _verticalSpeed = 0f;

        _selfRoomOverlaps = new Collider[20];
        _investigatorRoomOverlaps = new Collider[20];
        _clockOverlaps = new Collider[20];

        _clockOverlaps2 = new Collider[20];
        _playerOverlaps = new Collider[20];

        if (ServerGetTouchedInvestigators().Any())
            _isWaitingForInvestigatorsToLeaveTriggerAfterSpawn = true;

        _spawnPosition = transform.position;

        var tickSystem = TickSystem.Instance;
        _tickRate = tickSystem.TickRate;
        tickSystem.OnTick.AddListener(ServerTick);

        _footstepTimer = 0f;

        GameManager.OnClientGameOver.AddListener(OnServerGameOver);
    }

    [Server]
    public void OnServerGameOver(bool _)
    {
        if (!isServer)
            return;

        TickSystem.Instance.OnTick.RemoveListener(ServerTick);
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        if (HasAttacked)
            return;

        if (isClient)
        {
            Vector3 positionToSimulateFrom = _currentSimulatedPosition;
            TeleportTo(positionToSimulateFrom);
        }

        Simulate(_tickRate);

        if (isClient)
        {
            Vector3 positionToRollbackTo = _previousSimulatedPosition;
            TeleportTo(positionToRollbackTo);

            ClientReconcilePosition(_currentSimulatedPosition);
        }

        RpcReconcilePosition(_currentSimulatedPosition);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    public void RpcReconcilePosition(Vector3 position)
    {
        if (isServer)
            return;

        ClientReconcilePosition(position);
    }

    [Client]
    public void ClientReconcilePosition(Vector3 position)
    {
        if (!isClient)
            return;

        if (!isServer)
        {
            _previousSimulatedPosition = _currentSimulatedPosition;
            _currentSimulatedPosition = position;
        }

        _motionInterpolationTimer = 0f;
    }

    private void TeleportTo(Vector3 position)
    {
        _controller.enabled = false;
        transform.position = position;
        _controller.enabled = true;
    }

    private void Simulate(float deltaTime)
    {
        _previousSimulatedPosition = _currentSimulatedPosition;

        MoveVertically(_tickRate);
        CheckGround();

        bool isMoving = false;

        var targets = NeutralMode ? GetClocksInVisionRadius() : GetInvestigatorsInTheSameRoom();

        if (targets.Any())
        {
            _waitingBeforeGoingToSpawnPositionTimer = 0f;

            var closestTarget = targets
                .MinBy(target => Vector3.Distance(target.transform.position, transform.position))
                .FirstOrDefault();

            isMoving = true;
            MoveTo(closestTarget.transform.position, _tickRate);

            if (NeutralMode)
            {
                var touchedClocks = ServerGetTouchedClocks();

                if (touchedClocks.Any())
                {
                    HasAttacked = true;
                    _ = _animator.Bind(a => a.SetTrigger("Attack"));

                    var clockToDestroy = touchedClocks.UnityRandomItem();

                    if (clockToDestroy.TryGetComponent(out NetworkIdentity _))
                        NetworkServer.Destroy(clockToDestroy.gameObject);

                    GameManager.Instance.ServerDecreaseGameTimeBy(_gameTimeDecrease);
                    Invoke(nameof(ServerResetHasAttacked), _destroyDelay);
                }
            }
            else
            {
                if (ServerGetTouchedInvestigators().Any())
                {
                    if (_isWaitingForInvestigatorsToLeaveTriggerAfterSpawn)
                        return;

                    HasAttacked = true;
                    _ = _animator.Bind(a => a.SetTrigger("Attack"));

                    GameManager.Instance.ServerDecreaseGameTimeBy(_gameTimeDecrease);
                    Invoke(nameof(ServerDestroy), _destroyDelay);
                }
                else
                {
                    _isWaitingForInvestigatorsToLeaveTriggerAfterSpawn = false;
                }
            }
        }
        else
        {
            if (_waitingBeforeGoingToSpawnPositionTimer < _waitingTimeBeforeGoingToSpawnPosition)
            {
                _waitingBeforeGoingToSpawnPositionTimer += _tickRate;
            }
            else if (Vector3.Distance(transform.position, _spawnPosition) > 0.2f)
            {
                isMoving = true;
                MoveTo(_spawnPosition, _tickRate);
            }
        }

        _ = _animator.Bind(a => a.animator.SetBool("IsMoving", isMoving));

        if (isMoving && _isGrounded)
        {
            _footstepTimer += deltaTime;

            Debug.Log($"is moving and is grounded, _footstepTimer = {_footstepTimer}");

            if (_footstepTimer >= _footstepInterval)
            {
                ClientPlayFootstepSound();
                RpcPlayFootstepSound();

                _footstepTimer = 0f;
            }
        }
        else
        {
            _footstepTimer = 0f;
        }

        _currentSimulatedPosition = transform.position;
    }

    private IEnumerable<Transform> GetClocksInVisionRadius()
    {
        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: transform.position,
            radius: _clockVisionRadius,
            results: _clockOverlaps,
            layerMask: _clockLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        return _clockOverlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponentInParent(out Holdable holdable) ? holdable : null)
            .NonNullItems()
            .Where(h => h.Type is HoldableType.SandClock)
            .Select(h => h.transform);
    }

    private IEnumerable<Transform> GetInvestigatorsInTheSameRoom()
    {
        int selfRoomOverlapCount = Physics.OverlapSphereNonAlloc
        (
            position: transform.position,
            radius: 0.01f,
            results: _selfRoomOverlaps,
            layerMask: _roomLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Collide
        );

        var selfRooms = _selfRoomOverlaps
            .Take(selfRoomOverlapCount)
            .Select(o => o.TryGetComponent(out Room room) ? room : null)
            .NonNullItems();

        var investigators = FindObjectsByType<InvestigatorPlayerMovement>(FindObjectsSortMode.None).ToList();

        return investigators
            .Select(investigator =>
            {
                int investigatorRoomOverlapCount = Physics.OverlapSphereNonAlloc
                (
                    position: investigator.transform.position,
                    radius: 0.01f,
                    results: _investigatorRoomOverlaps,
                    layerMask: _roomLayerMask,
                    queryTriggerInteraction: QueryTriggerInteraction.Collide
                );

                var investigatorRooms = _investigatorRoomOverlaps
                    .Take(investigatorRoomOverlapCount)
                    .Select(o => o.TryGetComponent(out Room room) ? room : null)
                    .NonNullItems();

                return selfRooms.Intersect(investigatorRooms).Any() ? investigator : null;
            })
            .NonNullItems()
            .Select(investigator => investigator.transform);
    }

    private void MoveVertically(float deltaTime)
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

    private void CheckGround()
    {
        Vector3 groundCheckPosition = new
        (
            x: transform.position.x + _controller.center.x,
            y: BottomY + _controller.radius - _groundDistanceOffset,
            z: transform.position.z + _controller.center.z
        );

        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: groundCheckPosition,
            radius: _controller.radius,
            results: _groundOverlaps,
            layerMask: _groundMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        int overlapCountExcludingOneself = _groundOverlaps
            .Take(overlapCount)
            .Count(o => o.gameObject != gameObject);

        _isGrounded = overlapCountExcludingOneself > 0;
    }

    private void MoveTo(Vector3 targetPosition, float deltaTime)
    {
        Vector3 transformPositionWithoutY = new(transform.position.x, 0f, transform.position.z);
        Vector3 targetPositionWithoutY = new(targetPosition.x, 0f, targetPosition.z);

        Vector3 horizontalDirectionToTarget = (targetPositionWithoutY - transformPositionWithoutY).normalized;

        float rotationAngle = Mathf.Clamp
        (
            value: Vector3.SignedAngle(transform.forward, horizontalDirectionToTarget, Vector3.up),
            min: -_rotationSpeed * deltaTime,
            max: _rotationSpeed * deltaTime
        );

        transform.Rotate(Vector3.up, rotationAngle);

        _ = _controller.Move(_moveSpeed * deltaTime * transform.forward);
    }

    [Server]
    public void ServerResetHasAttacked()
    {
        if (!isServer)
            return;

        HasAttacked = false;
    }

    [Server]
    public void ServerDestroy()
    {
        if (!isServer)
            return;

        TickSystem.Instance.OnTick.RemoveListener(ServerTick);
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private IEnumerable<Transform> ServerGetTouchedClocks()
    {
        if (!isServer)
            return CreateList.With<Transform>();

        Vector3 point0 = new(transform.position.x, TopY - _controller.radius, transform.position.z);
        Vector3 point1 = new(transform.position.x, BottomY + _controller.radius, transform.position.z);
        float inflatedRadius = _controller.radius + _touchedTargetsCheckControllerRadiusInflation;

        int overlapCount = Physics.OverlapCapsuleNonAlloc
        (
            point0,
            point1,
            radius: inflatedRadius,
            results: _clockOverlaps2,
            layerMask: _clockLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        return _clockOverlaps2
            .Take(overlapCount)
            .Select(o => o.TryGetComponentInParent(out Holdable holdable) ? holdable : null)
            .NonNullItems()
            .Where(h => h.Type is HoldableType.SandClock)
            .Select(h => h.transform);
    }

    [Server]
    private IEnumerable<Transform> ServerGetTouchedInvestigators()
    {
        if (!isServer)
            return CreateList.With<Transform>();

        Vector3 point0 = new(transform.position.x, TopY - _controller.radius, transform.position.z);
        Vector3 point1 = new(transform.position.x, BottomY + _controller.radius, transform.position.z);
        float inflatedRadius = _controller.radius + _touchedTargetsCheckControllerRadiusInflation;

        int overlapCount = Physics.OverlapCapsuleNonAlloc
        (
            point0,
            point1,
            radius: inflatedRadius,
            results: _playerOverlaps,
            layerMask: _investigatorLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        return _playerOverlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out InvestigatorPlayerMovement investigatorPlayerMovement) ? investigatorPlayerMovement : null)
            .NonNullItems()
            .Select(i => i.transform);
    }

    [ClientRpc]
    public void RpcPlayFootstepSound()
    {
        if (isLocalPlayer)
            return;

        ClientPlayFootstepSound();
    }

    [Client]
    public void ClientPlayFootstepSound()
    {
        if (!isClient)
            return;

        _ = _audioController.Bind(c => c.PlayFootstepSound());
    }
}
