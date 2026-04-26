using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(InputManager))]
public abstract class PlayerMovement1 : NetworkBehaviour
{
    [Header("References")]

    [SerializeField] protected CharacterController _controller;
    [SerializeField] protected Transform _cameraRoot;
    [SerializeField] protected StaminaManager _staminaManager;
    [SerializeField] protected Transform _groundCheck;
    [SerializeField] protected PlayerAudioController _audioController;

    protected InputManager _inputManager;

    [Header("Ground Check")]

    [SerializeField] protected float _groundDistanceOffset = 0.05f;
    [SerializeField] protected LayerMask _groundMask;

    [Header("Gravity")]

    [SerializeField] protected float _terminalFallingVelocity = -50f;
    [SerializeField] protected float _defaultVerticalForce = -1.5f;

    [Header("Speed")]

    [SerializeField] protected float _currentSpeed = 1.5f;
    [SerializeField] protected float _walkingSpeed = 1.5f;
    [SerializeField] protected float _runningSpeed = 4.5f;

    protected bool _isGrounded;
    protected bool _wasGroundedLastFrame;

    protected float _verticalVelocity;

    protected Collider[] _groundOverlaps;

    protected PhysicsMaterial _currentPhysicsMaterial;
    protected PhysicsMaterial _lastPhysicsMaterial;

    private float BottomY => _controller.center.y - (_controller.height * 0.5f);

    public bool IsFalling => !_isGrounded && _verticalVelocity < 0f;

    private void Start()
    {
        _inputManager = GetComponent<InputManager>();

        _isGrounded = false;
        _wasGroundedLastFrame = false;

        _verticalVelocity = 0f;

        _groundOverlaps = new Collider[10];
    }

    private void Update()
    {
        CheckGround();
        SetPhysicsMaterial();
        HandleVerticalMovement();
        HandleLanding();
        HandleHorizontalMovement();

        _wasGroundedLastFrame = _isGrounded;
    }

    private void CheckGround()
    {
        _groundCheck.localPosition = new Vector3(0f, BottomY, 0f);

        int overlaps = Physics.OverlapSphereNonAlloc
        (
            position: _groundCheck.position + new Vector3(0f, _controller.radius - _groundDistanceOffset, 0f),
            radius: _controller.radius,
            results: _groundOverlaps,
            layerMask: _groundMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        _isGrounded = overlaps > 0;
    }

    private void SetPhysicsMaterial()
    {
        _currentPhysicsMaterial = _isGrounded ? _groundOverlaps[0].material : null;

        if (_currentPhysicsMaterial != _lastPhysicsMaterial)
            _audioController.SetMovementSounds(_currentPhysicsMaterial);

        _lastPhysicsMaterial = _currentPhysicsMaterial;
    }

    private void HandleVerticalMovement()
    {
        _verticalVelocity = _isGrounded
            ? _defaultVerticalForce
            : Mathf.Clamp
                (
                    value: _verticalVelocity + Physics.gravity.y * Time.deltaTime,
                    min: _terminalFallingVelocity,
                    max: float.PositiveInfinity
                );

        _ = _controller.Move(_verticalVelocity * Time.deltaTime * Vector3.up);
    }

    private void HandleLanding()
    {
        if (_isGrounded && !_wasGroundedLastFrame)
            _audioController.PlayLandingSound();
    }

    protected abstract void HandleHorizontalMovement();

    /*
    private void HandleHorizontalMovement()
    {
        float horizontal = _inputManager.Horizontal;
        float vertical = _inputManager.Vertical;

        Vector3 move = Vector3.ClampMagnitude(transform.right * horizontal + transform.forward * vertical, 1f);

        if (_inputManager.Sprint && !_stoppedRunning && _staminaManager.stamina > 0)
        {
            if (_isGrounded)
            {
                _currentSpeed = _runningSpeed;
                _isRunning = true;
            }
            else if(!_isRunning)
            {
                _currentSpeed = _walkingSpeed;
            }
        }
        else
        {
            if (_isRunning)
            {
                _isRunning = false;
                _stoppedRunning = true;
            }

            _currentSpeed = _walkingSpeed;
        }

        if (!_inputManager.Sprint && !_staminaManager.IsCriticalStamina)
            _stoppedRunning = false;

        if (_isGrounded && move.magnitude > 0f)
        {
            if (_isRunning)
                headBobController.Run();
            else
                headBobController.Walk();
        }
        else
        {
            headBobController.Reset();
        }

        _controller.Move(_currentSpeed * Time.deltaTime * move);
    }
    */
}
