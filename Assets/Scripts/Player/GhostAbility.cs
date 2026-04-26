using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public abstract class GhostAbility : NetworkBehaviour
{
    public UnityEvent<bool> OnCanBeActivatedChanged = new();
    public UnityEvent OnActivated = new();
    public UnityEvent OnStartCooldown = new();
    public UnityEvent<int> OnTimerUpdated = new();
    public UnityEvent OnStopCooldown = new();

    [SerializeField] private GhostAbilityData _data;
    public GhostAbilityData Data => _data;

    //[SerializeField] private GhostAudioController _audioController;

    [Space]

    [SyncVar(hook = nameof(OnClientIsActivatedChanged))]
    public bool IsActivated;

    [SyncVar(hook = nameof(OnClientIsCoolingDownChanged))]
    public bool IsCoolingDown;

    [SyncVar(hook = nameof(OnClientCanBeActivatedChanged))]
    public bool CanBeActivated;

    private GameHud _gameHud;
    private NetworkTimer _timer;

    private UnityAction _clientUnsubscribeFromTimerAction;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        IsActivated = false;
        IsCoolingDown = false;
        CanBeActivated = true;

        _timer = GameManager.Instance.ServerSpawnNetworkTimer();
    }

    [Client]
    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        _gameHud = FindAnyObjectByType<GameHud>();
    }

    [Client]
    public void OnClientIsActivatedChanged(bool oldValue, bool newValue)
    {
        if (!isLocalPlayer)
            return;

        if (oldValue == newValue)
            return;

        if (newValue)
        {
            OnActivated.Invoke();
            _ = _gameHud.Bind(hud => hud.SpawnGhostAbilityPopup(this));
        }

        // _ = _audioController.Bind(newValue ? c => c.PlayAbilityActivationSound() : c => c.PlayAbilityDeactivationSound());
    }

    [Client]
    public void OnClientIsCoolingDownChanged(bool oldValue, bool newValue)
    {
        if (!isLocalPlayer)
            return;

        if (oldValue == newValue)
            return;

        if (newValue)
            OnStartCooldown.Invoke();
        else
            OnStopCooldown.Invoke();
    }

    [Client]
    public void OnClientCanBeActivatedChanged(bool oldValue, bool newValue)
    {
        if (!isLocalPlayer)
            return;

        if (oldValue == newValue)
            return;

        OnCanBeActivatedChanged.Invoke(newValue);
    }

    [Command]
    public void CmdActivate()
    {
        if (!CanBeActivated || IsActivated || IsCoolingDown)
            return;

        _timer.ServerStopTimer();

        ServerDoActivation();

        IsActivated = true;

        if (_data.Duration > 0)
        {
            TargetRpcSubscribeToTimer(connectionToClient, _timer.netId);
            _timer.OnTimeRanOut.AddListener(ServerDeactivate);
            _timer.ServerStartTimer(_data.Duration);
        }
        else
        {
            ServerDeactivate();
        }
    }

    public abstract void ServerDoActivation();

    [Server]
    public void ServerDeactivate()
    {
        if (!isServer)
            return;

        ServerDoDeactivation();

        IsActivated = false;

        if (_data.Duration > 0)
        {
            _timer.OnTimeRanOut.RemoveListener(ServerDeactivate);
            _timer.ServerStopTimer();
        }

        ServerStartCooldown();
    }

    public abstract void ServerDoDeactivation();

    [Server]
    public void ServerStartCooldown()
    {
        if (!isServer)
            return;

        if (IsActivated || IsCoolingDown)
            return;

        IsCoolingDown = true;

        TargetRpcSubscribeToTimer(connectionToClient, _timer.netId);
        _timer.OnTimeRanOut.AddListener(ServerStopCooldown);
        _timer.ServerStartTimer(_data.Cooldown);
    }

    [Server]
    public void ServerStopCooldown()
    {
        if (!isServer)
            return;

        if (IsActivated || !IsCoolingDown)
            return;

        IsCoolingDown = false;

        _timer.OnTimeRanOut.RemoveListener(ServerStopCooldown);
    }

    [TargetRpc]
    public void TargetRpcSubscribeToTimer(NetworkConnectionToClient conn, uint timerNetId)
    {
        var timers = FindObjectsByType<NetworkTimer>(FindObjectsSortMode.None);
        var timerToSubscribeTo = timers.FirstOrDefault(timer => timer.netId == timerNetId);

        if (timerToSubscribeTo == null)
        {
            // We should leave the session here, because such situation should not happen at all.
            // TODO: leave session through LobbyNetworkManager and send the reason to server for it to send it to clients as a reason to terminate the session
            _ = SessionManager.Instance.LeaveSession();
            return;
        }

        _clientUnsubscribeFromTimerAction = () => ClientUnsubscribeFromTimer(timerToSubscribeTo);

        timerToSubscribeTo.OnUpdated.AddListener(ClientTimerUpdated);
        timerToSubscribeTo.OnTimeRanOut.AddListener(_clientUnsubscribeFromTimerAction);
    }

    [Client]
    public void ClientTimerUpdated(int seconds)
    {
        if (!isClient)
            return;

        OnTimerUpdated.Invoke(seconds);
    }

    [Client]
    public void ClientUnsubscribeFromTimer(NetworkTimer timer)
    {
        if (!isClient)
            return;

        timer.OnUpdated.RemoveListener(ClientTimerUpdated);

        if (_clientUnsubscribeFromTimerAction != null)
        {
            timer.OnTimeRanOut.RemoveListener(_clientUnsubscribeFromTimerAction);
            _clientUnsubscribeFromTimerAction = null;
        }
    }
}
