using System;
using System.Linq;
using Mirror;
using UnityEngine;

public class Compass : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClientHasTargetChanged))]
    public bool HasTarget;

    [SerializeField] private Transform _target;

    [Space]

    [SerializeField] private float _verticalDistanceToTargetThreshold = 1f;

    private GameHud _gameHud;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        HasTarget = false;

        GameManager.OnServerMainPhaseStarted.AddListener(ServerSetTarget);
        TickSystem.Instance.OnTick.AddListener(ServerTick);
    }

    [Client]
    public override void OnStartAuthority()
    {
        if (!isClient)
            return;

        _gameHud = FindAnyObjectByType<GameHud>();
    }

    [Server]
    public void ServerSetTarget()
    {
        if (!isServer)
            return;

        var investigator = FindAnyObjectByType<InvestigatorPlayerMovement>();

        if (investigator != null)
        {
            _target = investigator.transform;
            HasTarget = true;
            RpcSetTarget(investigator.netId);
        }
    }

    [ClientRpc]
    public void RpcSetTarget(uint targetNetId)
    {
        if (isServer || !isLocalPlayer)
            return;

        var investigators = FindObjectsByType<InvestigatorPlayerMovement>(FindObjectsSortMode.None);
        var investigatorToSetTargetAs = investigators.FirstOrDefault(investigator => investigator.netId == targetNetId);

        if (investigatorToSetTargetAs == null)
        {
            // We should leave the session here, because such situation should not happen at all.
            // TODO: leave session through LobbyNetworkManager and send the reason to server for it to send it to clients as a reason to terminate the session
            _ = SessionManager.Instance.LeaveSession();
            return;
        }

        _target = investigatorToSetTargetAs.transform;
    }

    [Server]
    public void ServerResetTarget()
    {
        if (!isServer)
            return;

        RpcResetTarget();
        HasTarget = false;
        _target = null;
    }

    [ClientRpc]
    public void RpcResetTarget()
    {
        if (isServer || !isLocalPlayer)
            return;

        _target = null;
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (!HasTarget || _target == null)
            return;

        Vector3 targetPositionWithoutY = new(_target.position.x, 0f, _target.position.z);
        Vector3 transformPositionWithoutY = new(transform.position.x, 0f, transform.position.z);

        Vector3 horizontalDirectionToTarget = (targetPositionWithoutY - transformPositionWithoutY).normalized;
        float targetHorizontalAngle = Vector3.SignedAngle(transform.forward, horizontalDirectionToTarget, Vector3.up);

        float verticalDistanceToTarget = Mathf.Abs(_target.position.y - transform.position.y);
        bool targetIsAtDifferentHeight = verticalDistanceToTarget > _verticalDistanceToTargetThreshold;

        ClientUpdateCompass(targetHorizontalAngle, targetIsAtDifferentHeight);
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        if (_target == null)
        {
            if (HasTarget)
                HasTarget = false;

            return;
        }
        else if (!HasTarget)
        {
            HasTarget = true;
        }

        Vector3 targetPositionWithoutY = new(_target.position.x, 0f, _target.position.z);
        Vector3 transformPositionWithoutY = new(transform.position.x, 0f, transform.position.z);

        Vector3 horizontalDirectionToTarget = (targetPositionWithoutY - transformPositionWithoutY).normalized;
        float targetHorizontalAngle = Vector3.SignedAngle(transform.forward, horizontalDirectionToTarget, Vector3.up);

        float verticalDistanceToTarget = Mathf.Abs(_target.position.y - transform.position.y);
        bool targetIsAtDifferentHeight = verticalDistanceToTarget > _verticalDistanceToTargetThreshold;

        if (isClient)
            ClientUpdateCompass(targetHorizontalAngle, targetIsAtDifferentHeight);

        RpcUpdateCompass(targetHorizontalAngle, targetIsAtDifferentHeight);
    }

    [Client]
    public void OnClientHasTargetChanged(bool oldValue, bool newValue)
    {
        if (!isLocalPlayer)
            return;

        if (oldValue == newValue)
            return;

        _ = _gameHud.Bind(newValue ? hud => hud.EnableCompass() : hud => hud.DisableCompass());
    }

    [ClientRpc]
    public void RpcUpdateCompass(float horizontalAngleToTarget, bool targetIsAtDifferentHeight)
    {
        if (isServer || !isLocalPlayer)
            return;

        ClientUpdateCompass(horizontalAngleToTarget, targetIsAtDifferentHeight);
    }

    [Client]
    public void ClientUpdateCompass(float horizontalAngleToTarget, bool targetIsAtDifferentHeight)
    {
        if (!isLocalPlayer)
            return;

        _ = _gameHud.Bind
        (
            (hud, horizontalAngle, isAtDifferentHeight) => hud.UpdateCompass(horizontalAngle, isAtDifferentHeight),
            horizontalAngleToTarget, targetIsAtDifferentHeight
        );
    }
}
