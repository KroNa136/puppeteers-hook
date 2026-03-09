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
    public static UnityEvent<float> OnClientWorldReconstructionProgressUpdated = new();
    public static UnityEvent OnClientWorldReconstructionCompleted = new();
    public static UnityEvent<int> OnClientTimerUpdated = new();
    public static UnityEvent OnClientGhostPreparePhaseStarted = new();
    public static UnityEvent OnClientMainPhaseStarted = new();
    public static UnityEvent OnClientGameOver = new();

    [SerializeField][Min(1)] private int _maxClientWorldReconstructionAttempts = 3;

    [Space]

    [SerializeField][Min(0)] private int _ghostSpawnAfterTime = 5;
    [SerializeField][Min(0)] private int _ghostPrepareTime = 30;
    [SerializeField][Min(0)] private int _mainGameTime = 300;

    [Space]

    [SerializeField] private GameObject _worldGeneratorPrefab;
    [SerializeField] private GameObject _networkTimerPrefab;

    private NetworkTimer _ghostSpawnTimer;
    private NetworkTimer _ghostPrepareTimer;
    private NetworkTimer _investigatorSpawnTimer;
    private NetworkTimer _mainTimer;

    private readonly List<NetworkConnectionToClient> _clientsThatSuccededToReconstructWorld = new();
    private readonly Dictionary<NetworkConnectionToClient, int> _clientWorldReconstructionAttempts = new();

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

        var worldGenerator = Instantiate(_worldGeneratorPrefab);
        NetworkServer.Spawn(worldGenerator);

        var networkTimersParent = GameObject.Find("Network Timers").transform;

        var ghostSpawnTimerObj = Instantiate(_networkTimerPrefab, networkTimersParent);
        NetworkServer.Spawn(ghostSpawnTimerObj);
        _ghostSpawnTimer = ghostSpawnTimerObj.GetComponent<NetworkTimer>();

        var ghostPrepareTimerObj = Instantiate(_networkTimerPrefab, networkTimersParent);
        NetworkServer.Spawn(ghostPrepareTimerObj);
        _ghostPrepareTimer = ghostPrepareTimerObj.GetComponent<NetworkTimer>();

        var investigatorSpawnTimerObj = Instantiate(_networkTimerPrefab, networkTimersParent);
        NetworkServer.Spawn(investigatorSpawnTimerObj);
        _investigatorSpawnTimer = investigatorSpawnTimerObj.GetComponent<NetworkTimer>();

        var mainTimerObj = Instantiate(_networkTimerPrefab, networkTimersParent);
        NetworkServer.Spawn(mainTimerObj);
        _mainTimer = mainTimerObj.GetComponent<NetworkTimer>();

        ServerStartGame();
    }

    [Server]
    public void ServerStartGame()
    {
        if (!isServer)
            return;

        if (LobbyNetworkManager.Instance.DEBUG_MODE)
        {
            var playersData = LobbyNetworkManager.Instance.ConnectedPlayers;
            playersData[0].Role = PlayerRole.Investigator;
            playersData[1].Role = PlayerRole.Ghost;
        }
        else
        {
            ServerAssignPlayerRoles();
        }

        RpcPlayerRolesAssigned();

        if (LobbyNetworkManager.Instance.DEBUG_MODE)
        {
            RpcWorldReconstructionCompleted();
            ServerStartPlayerSpawnTimers();
        }
        else
        {
            ServerStartWorldGeneration();
        }
    }

    [Server]
    public void ServerAssignPlayerRoles()
    {
        if (!isServer)
            return;

        var playersData = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

        int ghostIndex = UnityEngine.Random.Range(0, playersData.Length);

        for (int i = 0; i < playersData.Length; i++)
            playersData[i].Role = (i == ghostIndex) ? PlayerRole.Ghost : PlayerRole.Investigator;
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

        WorldGenerator.Instance.ServerGenerateWorld();
        WorldGenerator.OnGenerationProgressUpdated.AddListener(RpcWorldGenerationProgressUpdated);
        WorldGenerator.OnGenerationCompleted.AddListener(OnServerGeneratedWorld);
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
        ServerStartWorldReconstruction();
    }

    [ClientRpc]
    public void RpcWorldGenerationCompleted()
    {
        OnClientWorldGenerationCompleted.Invoke();
    }

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

    [Server]
    public void ServerStartPlayerSpawnTimers()
    {
        if (!isServer)
            return;

        foreach (var conn in _clientsThatSuccededToReconstructWorld)
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
    public void TargetRpcSubscribeToTimer(NetworkConnectionToClient conn, uint timerNetworkId)
    {
        var timers = FindObjectsByType<NetworkTimer>(FindObjectsSortMode.None);
        var timerToSubscribeTo = timers.FirstOrDefault(timer => timer.netId == timerNetworkId);

        if (timerToSubscribeTo == null)
        {
            // We should leave the session here, because such situation should not happen at all.
            // TODO: leave session through LobbyNetworkManager and send the reason to server for it to send it to clients as a reason to terminate the session
            NetworkClient.Disconnect();
            return;
        }

        timerToSubscribeTo.OnUpdated.AddListener(ClientTimerUpdated);
        timerToSubscribeTo.OnTimeRanOut.AddListener(() => ClientUnsubscribeFromTimer(timerToSubscribeTo));
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
    }

    [Server]
    public void ServerStartGhostPreparePhase()
    {
        if (!isServer)
            return;

        LobbyNetworkManager.Instance.ServerSpawnGhostPlayer();
        // TODO: FindAnyObjectByType<GhostPlayerMovement>().ServerSetDashSpeed();

        RpcGhostPreparePhaseStarted();

        foreach (var conn in _clientsThatSuccededToReconstructWorld)
        {
            var playerData = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn);

            // We are sure playerData is not null, since we have checked when starting the spawn timers.

            if (playerData.Role is not PlayerRole.Ghost)
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

        // TODO: FindAnyObjectByType<GhostPlayerMovement>().ServerSetWalkingSpeed();
        // TODO: teleport Ghost to a random room that's neither a starting room nor any adjacent room
        LobbyNetworkManager.Instance.ServerSpawnInvestigatorPlayer();

        RpcMainPhaseStarted();

        foreach (var conn in _clientsThatSuccededToReconstructWorld)
        {
            var playerData = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn);

            // We are sure playerData is not null, since we have checked when starting the spawn timers.

            TargetRpcSubscribeToTimer(conn, _mainTimer.netId);
        }

        _mainTimer.OnTimeRanOut.AddListener(ServerGameOver);
        _mainTimer.ServerStartTimer(_mainGameTime);

        _gameStarted = true;
    }

    [ClientRpc]
    public void RpcMainPhaseStarted()
    {
        OnClientMainPhaseStarted.Invoke();
    }

    [Server]
    private void ServerGameOver()
    {
        if (!isServer)
            return;

        if (!_gameStarted)
            return;

        // TODO: game ending logic

        RpcGameOver();
    }

    [ClientRpc]
    public void RpcGameOver()
    {
        OnClientGameOver.Invoke();
    }
}
