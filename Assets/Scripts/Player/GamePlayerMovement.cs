using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(CameraController))]
public abstract class GamePlayerMovement : NetworkBehaviour
{
    [SerializeField] protected Transform _cameraRoot;
    //[SerializeField] protected PlayerAudioController _audioController;

    protected CharacterController _controller;
    protected InputManager _inputManager;
    protected CameraController _cameraController;

    [Space]

    [SerializeField] private InputActionAsset _inputActions;

    [Space]

    [SerializeField] protected float _groundDistanceOffset = 0.05f;
    [SerializeField] protected LayerMask _groundMask;

    protected bool _wasGrounded;
    protected bool _isGrounded;
    protected Collider[] _groundOverlaps;

    protected PhysicsMaterial _previousPhysicsMaterial;
    protected PhysicsMaterial _currentPhysicsMaterial;

    [Space]

    [SerializeField] protected float _terminalFallingVelocity = -50f;
    [SerializeField] protected float _defaultVerticalForce = -1.5f;

    protected float _verticalSpeed;

    public float TopY => transform.position.y + _controller.center.y + (_controller.height * 0.5f);
    public float BottomY => transform.position.y + _controller.center.y - (_controller.height * 0.5f);

    public bool IsFalling => !_isGrounded && _verticalSpeed < 0f;

    [Space]

    [SerializeField] protected float _maxNetworkPositionError = 0.05f;
    [SerializeField] protected bool _smoothLocalMovement = true;
    [SerializeField] protected bool _smoothPositionErrorCorrection = false;
    [SerializeField] protected float _positionErrorCorrectionTime = 0.1f;

    protected readonly Queue<PlayerInputData> _clientInputBuffer = new();
    protected readonly Queue<PlayerInputData> _serverInputBuffer = new();
    protected readonly Queue<PlayerStateData> _stateBuffer = new();

    protected float _tickRate = 1f / 30f;

    protected Vector3 _previousSimulatedPosition;
    protected Vector3 _currentSimulatedPosition;
    protected float _motionInterpolationTimer;

    protected bool _isCorrectingPositionError;
    protected float _positionErrorCorrectionTimer;
    protected Vector3 _positionError;

    public bool CanBeControlledByPlayer = true;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _cameraController = GetComponent<CameraController>();

        _wasGrounded = false;
        _isGrounded = false;
        _groundOverlaps = new Collider[20];

        _previousPhysicsMaterial = null;
        _currentPhysicsMaterial = null;

        _verticalSpeed = 0f;

        var tickSystem = TickSystem.Instance;
        _tickRate = tickSystem.TickRate;
        tickSystem.OnTick.AddListener(Tick);

        _previousSimulatedPosition = transform.position;
        _currentSimulatedPosition = transform.position;
        _motionInterpolationTimer = 0f;

        _isCorrectingPositionError = false;
        _positionErrorCorrectionTimer = 0f;
        _positionError = Vector3.zero;

        GameManager.OnClientGameOver.AddListener(OnGameOver);

        OnStart();
    }

    private void OnGameOver(bool _)
    {
        CanBeControlledByPlayer = false;

        if (isLocalPlayer && Camera.main.transform.parent == _cameraRoot)
            Camera.main.transform.SetParent(null, worldPositionStays: true);
    }

    [Client]
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        var childRenderers = GetComponentsInChildren<Renderer>().AsEnumerable();

        if (TryGetComponent(out Renderer renderer))
            childRenderers = childRenderers.Prepend(renderer);

        childRenderers
            .NonNullItems()
            .ForEach(rend => rend.enabled = false);

        Camera.main.transform.SetParent(_cameraRoot);
        Camera.main.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        var playerInput = gameObject.AddComponent<PlayerInput>();
        playerInput.actions = _inputActions;
        playerInput.defaultActionMap = _inputActions.actionMaps[0].name;
        playerInput.neverAutoSwitchControlSchemes = false;
        playerInput.notificationBehavior = PlayerNotifications.SendMessages;
        playerInput.ActivateInput();

        _inputManager = gameObject.AddComponent<InputManager>();
    }

    private void Update()
    {
        if (!isClient)
            return;

        if (_smoothLocalMovement)
        {
            if (isLocalPlayer && CanBeControlledByPlayer)
                ClientSmoothMove();
            else
                ClientInterpolateMovement();
        }

        OnUpdate();
    }

    [Client]
    public void ClientSmoothMove()
    {
        if (!isLocalPlayer)
            return;

        if (!_smoothLocalMovement || !CanBeControlledByPlayer)
            return;

        // Here we move the player immediately just for visual smoothness.
        // When the client ticks, this movement will be used to calculate a "desired move" from the simulated position to the current position.
        // The desired move will be then simulated normally using tick rate.

        var (horizontal, vertical, sprint, sprintReleased) = _inputManager.GetOrDefault(im => (im.Horizontal, im.Vertical, im.Sprint, im.SprintReleased));

        Vector2 move = !CanBeControlledByPlayer ? Vector2.zero : new(horizontal, vertical);

        PlayerInputData input = new()
        {
            Tick = -1,
            Look = _cameraController.Look,
            Move = move,
            Sprint = sprint,
            SprintReleased = sprintReleased
        };

        PredictSimulation(input, Time.deltaTime);

        //Debug.Log($"visually moved client to {transform.position}");

        if (_isCorrectingPositionError)
            ClientCorrectPositionError();
    }

    [Client]
    public void ClientCorrectPositionError()
    {
        if (!isClient)
            return;

        if (!_smoothLocalMovement)
            return;

        if (!_smoothPositionErrorCorrection)
            return;

        if (!_isCorrectingPositionError)
            return;

        float tPrevious = Mathf.Clamp01(_positionErrorCorrectionTimer / _positionErrorCorrectionTime);

        _positionErrorCorrectionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_positionErrorCorrectionTimer / _positionErrorCorrectionTime);

        float tDelta = t - tPrevious;
        Vector3 desiredPositionDelta = tDelta * _positionError;
        Vector3 desiredPosition = transform.position + desiredPositionDelta;

        TeleportTo(desiredPosition);

        if (t >= 1f)
        {
            _isCorrectingPositionError = false;
            _positionError = Vector3.zero;
        }
    }

    [Client]
    public void ClientInterpolateMovement()
    {
        if (!isClient)
            return;

        if (isLocalPlayer)
            return;

        if (!_smoothLocalMovement)
            return;

        _motionInterpolationTimer += Time.deltaTime;

        float t = Mathf.Clamp01(_motionInterpolationTimer / _tickRate);
        //Debug.Log($"position interpolator = {t} ({_motionInterpolationTimer}/{_tickRate})");
        var targetPosition = Vector3.Lerp(_previousSimulatedPosition, _currentSimulatedPosition, t);

        TeleportTo(targetPosition);
    }

    private void Tick(int tick)
    {
        // First the client needs to tick, then the server.
        // This is necessary for simulating client-server-client communication on the host.

        if (isLocalPlayer)
            ClientTick(tick);

        if (isServer)
            ServerTick(tick);
    }

    [Client]
    public void ClientTick(int tick)
    {
        if (!isLocalPlayer)
            return;

        //Debug.Log($"client tick {tick}");

        Vector2 look = _cameraController.Look;

        var (horizontal, vertical, sprint, sprintReleased) = _inputManager.GetOrDefault(im => (im.Horizontal, im.Vertical, im.Sprint, im.SprintReleased));

        Vector2 move = !CanBeControlledByPlayer ? Vector2.zero :
            _smoothLocalMovement ? CalculateDesiredMove(_currentSimulatedPosition, transform.position, _tickRate) :
            new Vector2(horizontal, vertical);

        //Debug.Log($"simulated position {_currentSimulatedPosition} -> visual position {transform.position}, desired move: {move} (magnitude {move.magnitude})");

        PlayerInputData input = new()
        {
            Tick = tick,
            Look = look,
            Move = move,
            Sprint = sprint,
            SprintReleased = sprintReleased
        };

        _clientInputBuffer.Enqueue(input);
        //Debug.Log($"client saved input {input.Move} to buffer");

        if (_smoothLocalMovement)
        {
            TeleportTo(_currentSimulatedPosition);
            //Debug.Log($"tried to roll back to simulated position {_currentSimulatedPosition}, rolled back to {transform.position}");
        }

        //Debug.Log($"simulating input {input.Move} from client, we are on {transform.position} and looking in the direction {look} ...");
        Simulate(input, _tickRate);
        //Debug.Log($"finished simulating, ended up on {transform.position}");

        PlayerStateData state = new()
        {
            Tick = tick,
            Look = look,
            Position = transform.position
        };

        _stateBuffer.Enqueue(state);
        //Debug.Log($"[CLIENT] {state.Tick} {state.Position}");

        if (isServer)
            ServerProcessInput(input);
        else
            CmdProcessInput(input);

        OnClientTick(tick);
    }

    [Command]
    public void CmdProcessInput(PlayerInputData input)
    {
        ServerProcessInput(input);
    }

    [Server]
    public void ServerProcessInput(PlayerInputData input)
    {
        if (!isServer)
            return;

        input.Move.x = Mathf.Clamp(input.Move.x, -1f, 1f);
        input.Move.y = Mathf.Clamp(input.Move.y, -1f, 1f);

        _serverInputBuffer.Enqueue(input);
        //Debug.Log($"server saved input {input.Move} to buffer");
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        //Debug.Log($"server tick {tick}");

        if (_serverInputBuffer.Count == 0)
        {
            //Debug.Log("input buffer is empty, cancelling server tick");
            return;
        }

        Vector3 savedPosition = transform.position;
        Vector2 savedLook = _cameraController.Look;

        //Debug.Log($"saved visual position {savedPosition}");

        if (isClient)
        {
            Vector3 positionToSimulateFrom = isLocalPlayer ? _previousSimulatedPosition : _currentSimulatedPosition;
            TeleportTo(positionToSimulateFrom);

            //Debug.Log($"teleported to {(isLocalPlayer ? "previous" : "current")} simulated position {transform.position}");
        }

        PlayerInputData lastReceivedInput = new();

        while (_serverInputBuffer.TryDequeue(out var input))
        {
            _cameraController.SetLook(input.Look);

            BeforeServerSimulate(input, _tickRate);
            //Debug.Log($"simulating input {input.Move} from server, we are on {transform.position} and looking in the direction {_cameraController.Look} ...");
            Simulate(input, _tickRate);
            //Debug.Log($"finished simulating, ended up on {transform.position}");
            AfterServerSimulate(input, _tickRate);

            lastReceivedInput = input;
        }

        PlayerStateData state = new()
        {
            Tick = lastReceivedInput.Tick,
            Look = lastReceivedInput.Look,
            Position = transform.position
        };

        //Debug.Log($"[SERVER] {state.Tick} {state.Position}");

        if (isClient)
        {
            Vector3 positionToRollbackTo = isLocalPlayer ? savedPosition : _previousSimulatedPosition;
            TeleportTo(positionToRollbackTo);

            //Debug.Log($"rolled back to {(isLocalPlayer ? "visual" : "previous simulated")} position {transform.position}");

            if (isLocalPlayer)
                _cameraController.SetLook(savedLook);

            ClientReconcileState(state);
        }

        RpcReconcileState(state);

        OnServerTick(tick);
    }

    [ClientRpc]
    public void RpcReconcileState(PlayerStateData state)
    {
        if (isServer)
            return;

        ClientReconcileState(state);
    }

    [Client]
    public void ClientReconcileState(PlayerStateData serverState)
    {
        if (!isClient)
            return;

        if (isLocalPlayer)
        {
            int serverTick = serverState.Tick;

            while (_clientInputBuffer.TryPeek(out var input) && input.Tick <= serverTick)
                _ = _clientInputBuffer.Dequeue();

            while (_stateBuffer.TryPeek(out var state) && state.Tick < serverTick)
                _ = _stateBuffer.Dequeue();

            Vector3 positionOnServerTick = _stateBuffer.TryDequeue(out var stateOnServerTick)
                ? stateOnServerTick.Position
                // fallback to current position
                : transform.position;

            //Debug.Log($"client at position {transform.position} received server tick {serverTick}, we remember being at position {positionOnServerTick} and tick {stateOnServerTick.Tick}, server tells we should have been at {serverState.Position}");

            float error = Vector3.Distance(positionOnServerTick, serverState.Position);

            if (error < _maxNetworkPositionError)
                return;

            //Debug.Log("RECONCILIATION");

            Vector3 savedPosition = transform.position;
            Vector2 savedLook = _cameraController.Look;

            TeleportTo(serverState.Position);

            //Debug.Log($"tried to move client back to {serverState.Position}, moved to {transform.position}");

            _stateBuffer.Clear();

            while (_clientInputBuffer.TryDequeue(out var input))
            {
                _cameraController.SetLook(input.Look);

                //Debug.Log($"resimulating input {input.Move}, we are on {transform.position} and looking in the direction {_cameraController.Look} ...");
                Simulate(input, _tickRate);
                //Debug.Log($"finished resimulating, ended up on {transform.position}");

                PlayerStateData state = new()
                {
                    Tick = input.Tick,
                    Look = input.Look,
                    Position = transform.position
                };

                _stateBuffer.Enqueue(state);
            }

            if (_smoothLocalMovement && _smoothPositionErrorCorrection)
            {
                _isCorrectingPositionError = true;
                _positionError = transform.position - savedPosition;
                _positionErrorCorrectionTimer = 0f;

                TeleportTo(savedPosition);
            }

            _cameraController.SetLook(savedLook);
        }
        else
        {
            if (!isServer)
            {
                _ = _cameraController.Bind((controller, look) => controller.SetLook(look), serverState.Look);

                _previousSimulatedPosition = _currentSimulatedPosition;
                _currentSimulatedPosition = serverState.Position;

                //Debug.Log($"Set server authoritative positions {_previousSimulatedPosition} -> {_currentSimulatedPosition}");
            }

            _motionInterpolationTimer = 0f;
        }
    }

    [Server]
    public void ServerForceTeleportTo(Vector3 position)
    {
        if (!isServer)
            return;

        _serverInputBuffer.Clear();
        TeleportTo(position);

        _previousSimulatedPosition = _currentSimulatedPosition;
        _currentSimulatedPosition = transform.position;

        if (isClient)
            ClientForceTeleport();

        RpcForceTeleport();
    }

    [ClientRpc]
    public void RpcForceTeleport()
    {
        if (isServer)
            return;

        ClientForceTeleport();
    }

    [Client]
    public void ClientForceTeleport()
    {
        _clientInputBuffer.Clear();
        _stateBuffer.Clear();
    }

    protected void TeleportTo(Vector3 position)
    {
        //Vector3 teleportationMove = position - transform.position;
        //_ = _controller.Move(teleportationMove);
        _controller.enabled = false;
        transform.position = position;
        _controller.enabled = true;
    }

    private void Simulate(PlayerInputData input, float deltaTime)
    {
        _previousSimulatedPosition = _currentSimulatedPosition;
        //Debug.Log($"previuos simulated pos = {_previousSimulatedPosition}");

        MoveVertically(input, deltaTime);

        CheckGround();
        SetPhysicsMaterial();
        HandleLanding();

        Move(input, deltaTime);

        _currentSimulatedPosition = transform.position;
        //Debug.Log($"current simulated pos = {_currentSimulatedPosition}");
    }

    private void PredictSimulation(PlayerInputData input, float deltaTime)
    {
        MoveVertically(input, deltaTime);

        CheckGround();
        SetPhysicsMaterial();
        HandleLanding();

        Move(input, deltaTime);
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

        _wasGrounded = _isGrounded;
        _isGrounded = overlapCountExcludingOneself > 0;
    }

    private void SetPhysicsMaterial()
    {
        _previousPhysicsMaterial = _currentPhysicsMaterial;
        _currentPhysicsMaterial = _isGrounded ? _groundOverlaps[0].material : null;

        //if (_currentPhysicsMaterial != _previousPhysicsMaterial)
        //    _audioController.SetMovementSounds(_currentPhysicsMaterial);
    }

    private void HandleLanding()
    {
        //if (_isGrounded && !_wasGrounded)
        //    _audioController.PlayLandingSound();
    }

    protected virtual void OnStart() { }
    protected virtual void OnUpdate() { }

    protected virtual void OnClientTick(int tick) { }
    protected virtual void OnServerTick(int tick) { }

    protected virtual void BeforeServerSimulate(PlayerInputData input, float deltaTime) { }
    protected virtual void AfterServerSimulate(PlayerInputData input, float deltaTime) { }

    protected abstract void MoveVertically(PlayerInputData input, float deltaTime);
    protected abstract void Move(PlayerInputData input, float deltaTime);
    protected abstract Vector2 CalculateDesiredMove(Vector3 startPosition, Vector3 endPosition, float deltaTime);
}
