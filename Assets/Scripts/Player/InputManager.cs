using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    [SerializeField] private float _smoothInputSpeed = 0.01f;
    [SerializeField] private float _smoothInputDeadZone = 0.01f;

    private PlayerInput _playerInput;

    private InputAction _look;
    private InputAction _move;
    private InputAction _sprint;
    private InputAction _interact;
    private InputAction _flashlight;
    private InputAction _abilities;
    private InputAction _pause;

    public float LookX => _look != null ? _look.ReadValue<Vector2>().x : 0f;
    public float LookY => _look != null ? _look.ReadValue<Vector2>().y : 0f;
    public float Horizontal { get; private set; } = 0f;
    public float Vertical { get; private set; } = 0f;

    public bool Sprint => _sprint != null && _sprint.IsPressed();
    public bool SprintReleased => _sprint != null && _sprint.WasReleasedThisFrame();
    public bool Interact => _interact != null && _interact.WasPressedThisFrame();
    public bool Flashlight => _flashlight != null && _flashlight.WasPressedThisFrame();
    public bool Abilities => _abilities != null && _abilities.WasPressedThisFrame();
    public bool Pause => _pause != null && _pause.WasPressedThisFrame();

    private float _currentHorizontalInput = 0f;
    private float _currentVerticalInput = 0f;

    private float _currentSmoothHorizontalInputVelocity;
    private float _currentSmoothVerticalInputVelocity;

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();

        _look = _playerInput.actions["Look"];
        _move = _playerInput.actions["Move"];
        _sprint = _playerInput.actions["Sprint"];
        _interact = _playerInput.actions["Interact"];
        _flashlight = _playerInput.actions["Flashlight"];
        _abilities = _playerInput.actions["Abilities"];
        _pause = _playerInput.actions["Pause"];
    }

    private void Update()
    {
        Vector2 move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

        float horizontal = move.x;
        float vertical = move.y;

        _currentHorizontalInput = Mathf.SmoothDamp(_currentHorizontalInput, horizontal, ref _currentSmoothHorizontalInputVelocity, _smoothInputSpeed);
        _currentVerticalInput = Mathf.SmoothDamp(_currentVerticalInput, vertical, ref _currentSmoothVerticalInputVelocity, _smoothInputSpeed);

        if (Mathf.Abs(_currentHorizontalInput) < _smoothInputDeadZone)
            _currentHorizontalInput = 0f;

        if (Mathf.Abs(_currentVerticalInput) < _smoothInputDeadZone)
            _currentVerticalInput = 0f;

        Horizontal = _currentHorizontalInput;
        Vertical = _currentVerticalInput;
    }
}
