using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GhostPlayerMovement : GamePlayerMovement
{
    [SyncVar]
    [HideInInspector] public bool CanDash;

    [SyncVar]
    [HideInInspector] public bool IsPreparingToDash;

    [SyncVar]
    [HideInInspector] public bool IsDashing;

    [SyncVar]
    [HideInInspector] public Vector3 DashDirection;

    // [SerializeField] private GhostAudioController _ghostAudioController;

    [Space]

    [SerializeField] private float _walkingSpeed = 0.75f;
    [SerializeField] private float _dashSpeed = 6f;

    [Space]

    [SerializeField] private float _dashHoldTimeRequired = 1f;
    [SerializeField] private float _dashPrepareDuration = 1f;
    [SerializeField] private float _dashDuration = 1f;
    [SerializeField] private float _dashCooldownDuration = 40f;

    private float _dashHoldTime = 0f;
    private bool _sentDashCommand = false;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        base.OnStartServer();

        CanDash = true;
        IsDashing = false;
    }

    protected override void OnUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (_inputManager.Sprint && !IsDashing && CanDash && !_sentDashCommand)
        {
            _dashHoldTime += Time.deltaTime;

            if (_dashHoldTime >= _dashHoldTimeRequired)
            {
                CmdDash();
                _sentDashCommand = true;
            }
        }
        else
        {
            _dashHoldTime = 0f;
        }
    }

    [Command]
    public void CmdDash()
    {
        if (IsPreparingToDash || IsDashing || !CanDash)
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

        IsPreparingToDash = true;
        RpcPlayDashPrepareSound();

        yield return new WaitForSeconds(_dashPrepareDuration);

        IsPreparingToDash = false;
        IsDashing = true;
        RpcPlayDashSound();

        DashDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);

        yield return new WaitForSeconds(_dashDuration);

        IsDashing = false;

        yield return new WaitForSeconds(_dashCooldownDuration);

        CanDash = true;
    }

    [ClientRpc]
    public void RpcPlayDashPrepareSound()
    {
        //_audioController.PlayDashPrepareSound();
    }

    [ClientRpc]
    public void RpcPlayDashSound()
    {
        //_audioController.PlayDashSound();
    }

    [TargetRpc]
    public void TargetRpcResetDashCommand(NetworkConnectionToClient conn)
    {
        _sentDashCommand = false;
    }

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
        float speed = IsDashing ? _dashSpeed : _walkingSpeed;
        Vector3 clampedMove = Vector3.ClampMagnitude
        (
            vector: IsDashing ?
                DashDirection :
                transform.right * input.Move.x + transform.forward * input.Move.y,
            maxLength: 1f
        );

        _ = _controller.Move(speed * deltaTime * clampedMove);
    }

    protected override Vector2 CalculateDesiredMove(Vector3 startPosition, Vector3 endPosition, float deltaTime)
    {
        if (IsDashing)
            return Vector2.zero;

        Vector3 startPositionWithoutY = new(startPosition.x, 0f, startPosition.z);
        Vector3 endPositionWithoutY = new(endPosition.x, 0f, endPosition.z);

        Vector3 positionDelta = endPositionWithoutY - startPositionWithoutY;

        float forwardProjection = Vector3.Dot(positionDelta, transform.forward);
        float rightProjection = Vector3.Dot(positionDelta, transform.right);

        return new Vector2(rightProjection, forwardProjection) / (_walkingSpeed * deltaTime);
    }
}
