using Mirror;
using UnityEngine;

[RequireComponent(typeof(InvestigatorStaminaManager))]
public class InvestigatorPlayerMovement : GamePlayerMovement
{
    [SyncVar]
    public bool StoppedSprintingAtCriticalStamina = false;

    [Space]

    [SerializeField] private float _walkingSpeed = 1.5f;
    [SerializeField] private float _runningSpeed = 3f;

    [Space]

    [SerializeField] private float _walkingFootstepInterval = 0.4f;
    [SerializeField] private float _runningFootstepInterval = 0.2f;

    protected override float FootstepInterval => _isRunning ? _runningFootstepInterval : _walkingFootstepInterval;

    private InvestigatorStaminaManager _staminaManager;

    private bool CanRun => !_staminaManager.IsCriticalStamina || !StoppedSprintingAtCriticalStamina;
    private bool _isRunning = false;

    protected override void OnStart()
    {
        _staminaManager = GetComponent<InvestigatorStaminaManager>();
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        base.OnStartServer();

        StoppedSprintingAtCriticalStamina = false;
    }

    [Server]
    protected override void BeforeServerSimulate(PlayerInputData input, float deltaTime)
    {
        if (!isServer)
            return;

        StoppedSprintingAtCriticalStamina = StoppedSprintingAtCriticalStamina
            ? _staminaManager.IsCriticalStamina
            : _staminaManager.CurrentStamina == 0f || (_staminaManager.IsCriticalStamina && input.SprintReleased);
    }

    [Server]
    protected override void AfterServerSimulate(PlayerInputData input, float deltaTime)
    {
        if (!isServer)
            return;

        bool isMoving = input.Move.sqrMagnitude > 0.001f;
        _isRunning = input.Sprint && CanRun;

        _ = isMoving && _isRunning
            ? _staminaManager.Bind((sm, dt) => sm.ServerDrain(dt), deltaTime)
            : _staminaManager.Bind((sm, dt, isMoving) => sm.ServerRegenerate(dt, isMoving), deltaTime, isMoving);

        int moveMode = isMoving ? (_isRunning ? 2 : 1) : 0;
        _ = _animator.Bind(a => a.animator.SetInteger("MoveMode", moveMode));
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
        _isRunning = input.Sprint && CanRun;
        float speed = _isRunning ? _runningSpeed : _walkingSpeed;

        Vector3 clampedMove = Vector3.ClampMagnitude(transform.right * input.Move.x + transform.forward * input.Move.y, 1f);

        _ = _controller.Move(speed * deltaTime * clampedMove);
    }

    protected override Vector2 CalculateDesiredMove(Vector3 startPosition, Vector3 endPosition, float deltaTime)
    {
        bool isRunning = _inputManager.Sprint && (!_staminaManager.IsCriticalStamina || !StoppedSprintingAtCriticalStamina);
        float speed = isRunning ? _runningSpeed : _walkingSpeed;

        Vector3 startPositionWithoutY = new(startPosition.x, 0f, startPosition.z);
        Vector3 endPositionWithoutY = new(endPosition.x, 0f, endPosition.z);

        Vector3 positionDelta = endPositionWithoutY - startPositionWithoutY;

        float forwardProjection = Vector3.Dot(positionDelta, transform.forward);
        float rightProjection = Vector3.Dot(positionDelta, transform.right);

        return new Vector2(rightProjection, forwardProjection) / (speed * deltaTime);
    }
}
