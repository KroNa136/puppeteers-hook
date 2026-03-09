using UnityEngine;

public class InvestigatorPlayerMovement : GamePlayerMovement
{
    [Space]

    [SerializeField] private float _walkingSpeed = 1.5f;
    [SerializeField] private float _runningSpeed = 3f;

    protected override void MoveVertically(PlayerInputData input, float deltaTime)
    {
        _verticalSpeed = _isGrounded ?
            _defaultVerticalForce :
            Mathf.Clamp
            (
                value: _verticalSpeed + Physics.gravity.y * deltaTime,
                min: _terminalFallingVelocity,
                max: float.PositiveInfinity
            );

        _ = _controller.Move(_verticalSpeed * deltaTime * Vector3.up);
    }

    protected override void Move(PlayerInputData input, float deltaTime)
    {
        float speed = input.Sprint ? _runningSpeed : _walkingSpeed;
        Vector3 clampedMove = Vector3.ClampMagnitude(transform.right * input.Move.x + transform.forward * input.Move.y, 1f);

        _ = _controller.Move(speed * deltaTime * clampedMove);
    }

    protected override Vector2 CalculateDesiredMove(Vector3 startPosition, Vector3 endPosition, float deltaTime)
    {
        float speed = _inputManager.Sprint ? _runningSpeed : _walkingSpeed;

        Vector3 startPositionWithoutY = new(startPosition.x, 0f, startPosition.z);
        Vector3 endPositionWithoutY = new(endPosition.x, 0f, endPosition.z);

        Vector3 positionDelta = endPositionWithoutY - startPositionWithoutY;

        float forwardProjection = Vector3.Dot(positionDelta, transform.forward);
        float rightProjection = Vector3.Dot(positionDelta, transform.right);

        return new Vector2(rightProjection, forwardProjection) / (speed * deltaTime);
    }

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
