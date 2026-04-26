using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public static UnityEvent OnClientPlayerRolesAssigned = new();
    public static UnityEvent<float> OnClientWorldGenerationProgressUpdated = new();
    public static UnityEvent OnClientWorldGenerationCompleted = new();
    //public static UnityEvent<float> OnClientWorldReconstructionProgressUpdated = new();
    //public static UnityEvent OnClientWorldReconstructionCompleted = new();
    public static UnityEvent<int> OnClientTimerUpdated = new();
    public static UnityEvent OnClientGhostPreparePhaseStarted = new();
    public static UnityEvent OnServerMainPhaseStarted = new();
    public static UnityEvent OnClientMainPhaseStarted = new();
    public static UnityEvent<int> OnClientGameTimeDecreased = new();
    public static UnityEvent<bool> OnClientGameOver = new();

    //[SerializeField][Min(1)] private int _maxClientWorldReconstructionAttempts = 3;

    [Space]

    [SerializeField][Min(0)] private int _ghostSpawnAfterTime = 5;
    [SerializeField][Min(0)] private int _ghostPrepareTime = 60;
    [SerializeField][Min(0)] private int _mainGameTime = 600;

    [Space]

    private NetworkTimer _ghostSpawnTimer;
    private NetworkTimer _ghostPrepareTimer;
    private NetworkTimer _investigatorSpawnTimer;
    private NetworkTimer _mainTimer;

    private Transform _networkTimersParent;

    //private readonly List<NetworkConnectionToClient> _clientsThatSuccededToReconstructWorld = new();
    //private readonly Dictionary<NetworkConnectionToClient, int> _clientWorldReconstructionAttempts = new();

    private bool _gameStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        Invoke(nameof(ServerInitializeAndStartGame), 1f);
    }

    [Server]
    public void ServerInitializeAndStartGame()
    {
        if (!isServer)
            return;

        var worldGenerator = Instantiate(LobbyNetworkManager.Instance.WorldGeneratorPrefab);
        NetworkServer.Spawn(worldGenerator);

        _networkTimersParent = GameObject.Find("Network Timers").transform;

        _ghostSpawnTimer = ServerSpawnNetworkTimer();
        _ghostPrepareTimer = ServerSpawnNetworkTimer();
        _investigatorSpawnTimer = ServerSpawnNetworkTimer();
        _mainTimer = ServerSpawnNetworkTimer();

        ServerStartGame();
    }

    [Server]
    public NetworkTimer ServerSpawnNetworkTimer()
    {
        if (!isServer)
            return null;

        if (LobbyNetworkManager.Instance.DEBUG_MODE)
        {
            _ghostSpawnAfterTime = 1;
            _ghostPrepareTime = 5;
        }

        var timerObj = _networkTimersParent != null
            ? Instantiate(LobbyNetworkManager.Instance.NetworkTimerPrefab, _networkTimersParent)
            : Instantiate(LobbyNetworkManager.Instance.NetworkTimerPrefab);

        NetworkServer.Spawn(timerObj);

        return timerObj.GetComponent<NetworkTimer>();
    }

    [Server]
    public void ServerStartGame()
    {
        if (!isServer)
            return;

        if (LobbyNetworkManager.Instance.DEBUG_MODE)
        {
            var playersData = LobbyNetworkManager.Instance.ConnectedPlayersData;
            playersData[0].Role = PlayerRole.Investigator;
            playersData[1].Role = PlayerRole.Ghost;
        }
        else
        {
            ServerAssignPlayerRoles();
        }

        RpcPlayerRolesAssigned();
        ServerStartWorldGeneration();

        /*
        if (LobbyNetworkManager.Instance.DEBUG_MODE)
        {
            RpcWorldGenerationCompleted();
            ServerStartPlayerSpawnTimers();
        }
        else
        {
            ServerStartWorldGeneration();
        }
        */
    }

    [Server]
    public void ServerAssignPlayerRoles()
    {
        if (!isServer)
            return;

        var playersData = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

        var ghostData = playersData.UnityRandomItem();
        ghostData.Role = PlayerRole.Ghost;

        foreach (var playerData in playersData.Without(ghostData))
            playerData.Role = PlayerRole.Investigator;
    }

    [ClientRpc]
    public void RpcPlayerRolesAssigned()
    {
        OnClientPlayerRolesAssigned.Invoke();
    }

    [Server]
    public void ServerStartWorldGeneration()
    {
        if (!isServer)
            return;

        WorldGenerator.OnGenerationProgressUpdated.AddListener(RpcWorldGenerationProgressUpdated);
        WorldGenerator.OnGenerationCompleted.AddListener(OnServerGeneratedWorld);
        WorldGenerator.Instance.ServerGenerateWorld();
    }

    [ClientRpc]
    public void RpcWorldGenerationProgressUpdated(float value)
    {
        OnClientWorldGenerationProgressUpdated.Invoke(value);
    }

    [Server]
    public void OnServerGeneratedWorld()
    {
        if (!isServer)
            return;

        RpcWorldGenerationCompleted();
        //ServerStartWorldReconstruction();
        ServerStartPlayerSpawnTimers();
    }

    [ClientRpc]
    public void RpcWorldGenerationCompleted()
    {
        OnClientWorldGenerationCompleted.Invoke();
    }

    /*
    [Server]
    public void ServerStartWorldReconstruction()
    {
        if (!isServer)
            return;

        WorldGenerator.Instance.RpcReconstructWorld(WorldGenerator.Instance.WorldData, WorldGenerator.Instance.WorldHash);
        RpcAddWorldReconstructionProgressUpdatedListener();
        WorldGenerator.OnReconstructionCompleted.AddListener(OnServerReconstructedWorld);
    }

    [ClientRpc]
    public void RpcAddWorldReconstructionProgressUpdatedListener()
    {
        WorldGenerator.OnReconstructionProgressUpdated.AddListener(ClientWorldReconstructionProgressUpdated);
    }

    [Client]
    public void ClientWorldReconstructionProgressUpdated(float value)
    {
        if (!isClient)
            return;

        OnClientWorldReconstructionProgressUpdated.Invoke(value);
    }

    [Server]
    public void OnServerReconstructedWorld(NetworkConnectionToClient conn, bool success)
    {
        if (!isServer)
            return;

        if (_clientsThatSuccededToReconstructWorld.Count >= LobbyNetworkManager.Instance.ConnectedPlayers.Count)
            return;

        if (success)
        {
            _clientsThatSuccededToReconstructWorld.Add(conn);

            if (_clientsThatSuccededToReconstructWorld.Count == LobbyNetworkManager.Instance.ConnectedPlayers.Count)
            {
                RpcWorldReconstructionCompleted();
                ServerStartPlayerSpawnTimers();
            }
        }
        else
        {
            if (_clientWorldReconstructionAttempts.ContainsKey(conn))
            {
                _clientWorldReconstructionAttempts[conn]++;

                if (_clientWorldReconstructionAttempts[conn] >= _maxClientWorldReconstructionAttempts)
                {
                    // Either one of the clients has a bad connection, or it is corrupted or hacked, or there is a MITM attack going on.
                    // In any case, we should terminate the session.
                    // TODO: stop session through LobbyNetworkManager and send the reason to clients
                    conn.Disconnect();
                }
                else
                {
                    WorldGenerator.Instance.TargetRpcReconstructWorld(conn, WorldGenerator.Instance.WorldData, WorldGenerator.Instance.WorldHash);
                }
            }
            else
            {
                _clientWorldReconstructionAttempts.Add(conn, 1);
            }
        }
    }

    [ClientRpc]
    public void RpcWorldReconstructionCompleted()
    {
        OnClientWorldReconstructionCompleted.Invoke();
    }
    */

    [Server]
    public void ServerStartPlayerSpawnTimers()
    {
        if (!isServer)
            return;

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers /*_clientsThatSuccededToReconstructWorld*/)
        {
            var playerData = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn);

            if (playerData == null || playerData.Role is PlayerRole.None)
            {
                // We should terminate the session here, because somehow we have an unregistered or unconfigured client.
                // TODO: stop session through LobbyNetworkManager and send the reason to clients
                conn.Disconnect();
                return;
            }

            var timerToSubscribeTo = playerData.Role switch
            {
                PlayerRole.Ghost => _ghostSpawnTimer,
                PlayerRole.Investigator => _investigatorSpawnTimer,
                _ => throw new InvalidOperationException("Unsupported PlayerRole value")
            };

            TargetRpcSubscribeToTimer(conn, timerToSubscribeTo.netId);
        }

        _ghostSpawnTimer.OnTimeRanOut.AddListener(ServerStartGhostPreparePhase);
        _ghostSpawnTimer.ServerStartTimer(_ghostSpawnAfterTime);

        _investigatorSpawnTimer.ServerStartTimer(_ghostSpawnAfterTime + _ghostPrepareTime);
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

        timerToSubscribeTo.OnUpdated.AddListener(ClientTimerUpdated);
        timerToSubscribeTo.OnTimeRanOut.AddListener(() => ClientUnsubscribeFromTimer(timerToSubscribeTo));
    }

    [TargetRpc]
    public void TargetRpcUnsubscribeFromTimer(NetworkConnectionToClient conn, uint timerNetId)
    {
        var timers = FindObjectsByType<NetworkTimer>(FindObjectsSortMode.None);
        var timerToUnsubscribeFrom = timers.FirstOrDefault(timer => timer.netId == timerNetId);

        if (timerToUnsubscribeFrom == null)
        {
            // We should leave the session here, because such situation should not happen at all.
            // TODO: leave session through LobbyNetworkManager and send the reason to server for it to send it to clients as a reason to terminate the session
            _ = SessionManager.Instance.LeaveSession();
            return;
        }

        ClientUnsubscribeFromTimer(timerToUnsubscribeFrom);
    }

    [Client]
    public void ClientTimerUpdated(int seconds)
    {
        if (!isClient)
            return;

        OnClientTimerUpdated.Invoke(seconds);
    }

    [Client]
    public void ClientUnsubscribeFromTimer(NetworkTimer timer)
    {
        if (!isClient)
            return;

        timer.OnUpdated.RemoveListener(ClientTimerUpdated);
        timer.OnTimeRanOut.RemoveListener(() => ClientUnsubscribeFromTimer(timer));
    }

    [Server]
    public void ServerStartGhostPreparePhase()
    {
        if (!isServer)
            return;

        var unlockedDoors = FindObjectsByType<Door>(FindObjectsSortMode.None)
            .Where(door => !door.IsLocked);

        foreach (var door in unlockedDoors)
            door.ServerOpen();

        LobbyNetworkManager.Instance.ServerSpawnGhostPlayer();

        var ghost = FindAnyObjectByType<GhostPlayerMovement>();
        ghost.CanDash = false;
        ghost.IsInPreparePhase = true;

        var ghostAbilities = ghost.GetComponents<GhostAbility>();

        foreach (var ability in ghostAbilities)
            ability.CanBeActivated = false;

        RpcGhostPreparePhaseStarted();

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers /*_clientsThatSuccededToReconstructWorld*/)
        {
            var role = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn).Role;

            if (role is not PlayerRole.Ghost)
                continue;

            TargetRpcSubscribeToTimer(conn, _ghostPrepareTimer.netId);
        }

        _ghostPrepareTimer.OnTimeRanOut.AddListener(ServerStartMainPhase);
        _ghostPrepareTimer.ServerStartTimer(_ghostPrepareTime);
    }

    [ClientRpc]
    public void RpcGhostPreparePhaseStarted()
    {
        OnClientGhostPreparePhaseStarted.Invoke();
    }

    [Server]
    private void ServerStartMainPhase()
    {
        if (!isServer)
            return;

        var openedDoors = FindObjectsByType<Door>(FindObjectsSortMode.None)
            .Where(door => door.IsOpened);

        foreach (var door in openedDoors)
            door.ServerClose();

        var ghost = FindAnyObjectByType<GhostPlayerMovement>();
        ghost.CanDash = true;
        ghost.IsInPreparePhase = false;

        var ghostAbilities = ghost.GetComponents<GhostAbility>();

        foreach (var ability in ghostAbilities)
            ability.CanBeActivated = true;

        var roomToTeleportGhostTo = FindObjectsByType<Room>(FindObjectsSortMode.None)
            .Where(r => r.Type is not RoomType.MainHall)
            .Where(r => r.Neighbors.None(n => n.Type is RoomType.MainHall))
            .UnityRandomItem();

        ghost.ServerForceTeleportTo(roomToTeleportGhostTo.PlayerSpawnPosition);

        LobbyNetworkManager.Instance.ServerSpawnInvestigatorPlayer();

        OnServerMainPhaseStarted.Invoke();
        RpcMainPhaseStarted();

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers /*_clientsThatSuccededToReconstructWorld*/)
            TargetRpcSubscribeToTimer(conn, _mainTimer.netId);

        _mainTimer.OnTimeRanOut.AddListener(ServerGhostWin);
        _mainTimer.ServerStartTimer(_mainGameTime);

        _gameStarted = true;
    }

    [ClientRpc]
    public void RpcMainPhaseStarted()
    {
        OnClientMainPhaseStarted.Invoke();
    }

    [Server]
    public void ServerDecreaseGameTimeBy(int seconds)
    {
        if (!isServer)
            return;

        _mainTimer.ServerDecreaseTimeBy(seconds);
        RpcGameTimeDecreased(seconds);
    }

    [ClientRpc]
    public void RpcGameTimeDecreased(int seconds)
    {
        OnClientGameTimeDecreased.Invoke(seconds);
    }

    [Server]
    public void ServerInvestigatorWin()
    {
        if (!isServer)
            return;

        if (!_gameStarted)
            return;

        _mainTimer.ServerStopTimer();

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers /*_clientsThatSuccededToReconstructWorld*/)
        {
            var role = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn).Role;

            bool win = role switch
            {
                PlayerRole.Ghost => false,
                PlayerRole.Investigator => true,
                _ => throw new InvalidOperationException("Unsupported PlayerRole value")
            };

            TargetRpcUnsubscribeFromTimer(conn, _mainTimer.netId);
            TargetRpcGameOver(conn, win);
        }
    }

    [Server]
    public void ServerGhostWin()
    {
        if (!isServer)
            return;

        if (!_gameStarted)
            return;

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers /*_clientsThatSuccededToReconstructWorld*/)
        {
            var role = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn).Role;

            bool win = role switch
            {
                PlayerRole.Ghost => true,
                PlayerRole.Investigator => false,
                _ => throw new InvalidOperationException("Unsupported PlayerRole value")
            };

            TargetRpcGameOver(conn, win);
        }
    }

    [TargetRpc]
    public void TargetRpcGameOver(NetworkConnectionToClient conn, bool win)
    {
        OnClientGameOver.Invoke(win);
    }
}
