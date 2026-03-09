using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(InputManager), typeof(CameraController))]
public abstract class GamePlayerMovement : NetworkBehaviour
{
    [SerializeField] protected Transform _cameraRoot;
    //[SerializeField] protected PlayerAudioController _audioController;

    protected CharacterController _controller;
    protected InputManager _inputManager;
    protected CameraController _cameraController;

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

    protected float BottomY => _controller.center.y - (_controller.height * 0.5f);

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

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _inputManager = GetComponent<InputManager>();
        _cameraController = GetComponent<CameraController>();

        _wasGrounded = false;
        _isGrounded = false;
        _groundOverlaps = new Collider[10];

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

        OnStart();
    }

    [Client]
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer)
            return;

        Camera.main.transform.SetParent(_cameraRoot);
        Camera.main.transform.localPosition = Vector3.zero;
    }

    private void Update()
    {
        if (!isClient)
            return;

        if (_smoothLocalMovement)
        {
            if (isLocalPlayer)
                ClientSmoothMove();
            else
                ClientInterpolateMovement();
        }

        OnUpdate();
    }

    [Client]
    public void ClientSmoothMove()
    {
        if (!isClient)
            return;

        if (!_smoothLocalMovement)
            return;

        // Here we move the player immediately just for visual smoothness.
        // When the client ticks, this movement will be used to calculate a "desired move" from the simulated position to the current position.
        // The desired move will be then simulated normally using tick rate.

        Vector2 move = new(_inputManager.Horizontal, _inputManager.Vertical);

        PlayerInputData input = new()
        {
            Tick = -1,
            Look = _cameraController.Look,
            Move = move,
            Sprint = _inputManager.Sprint
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

        Vector2 move = _smoothLocalMovement ?
            CalculateDesiredMove(_currentSimulatedPosition, transform.position, _tickRate) :
            new Vector2(_inputManager.Horizontal, _inputManager.Vertical);

        //Debug.Log($"simulated position {_currentSimulatedPosition} -> visual position {transform.position}, desired move: {move} (magnitude {move.magnitude})");

        PlayerInputData input = new()
        {
            Tick = tick,
            Look = look,
            Move = move,
            Sprint = _inputManager.Sprint
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

            //Debug.Log($"simulating input {input.Move} from server, we are on {transform.position} and looking in the direction {_cameraController.Look} ...");
            Simulate(input, _tickRate);
            //Debug.Log($"finished simulating, ended up on {transform.position}");

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

            Vector3 positionOnServerTick = _stateBuffer.TryDequeue(out var stateOnServerTick) ?
                stateOnServerTick.Position :
                // fallback to current position
                transform.position;

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
                if (_cameraController != null)
                    _cameraController.SetLook(serverState.Look);

                _previousSimulatedPosition = _currentSimulatedPosition;
                _currentSimulatedPosition = serverState.Position;

                //Debug.Log($"Set server authoritative positions {_previousSimulatedPosition} -> {_currentSimulatedPosition}");
            }

            _motionInterpolationTimer = 0f;
        }
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
        Vector3 groundCheckPosition = new(0f, BottomY + _controller.radius - _groundDistanceOffset, 0f);

        int overlapCount = Physics.OverlapSphereNonAlloc
        (
            position: groundCheckPosition,
            radius: _controller.radius,
            results: _groundOverlaps,
            layerMask: _groundMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        _wasGrounded = _isGrounded;
        _isGrounded = overlapCount > 0;
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

    protected abstract void MoveVertically(PlayerInputData input, float deltaTime);
    protected abstract void Move(PlayerInputData input, float deltaTime);
    protected abstract Vector2 CalculateDesiredMove(Vector3 startPosition, Vector3 endPosition, float deltaTime);
}
