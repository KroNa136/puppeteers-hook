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
    public static UnityEvent<int> OnClientTimerUpdated = new();
    public static UnityEvent OnServerGhostPreparePhaseStarted = new();
    public static UnityEvent OnClientGhostPreparePhaseStarted = new();
    public static UnityEvent OnServerMainPhaseStarted = new();
    public static UnityEvent OnClientMainPhaseStarted = new();
    public static UnityEvent<int> OnClientGameTimeDecreased = new();
    public static UnityEvent OnServerGameOver = new();
    public static UnityEvent<bool> OnClientGameOver = new();

    [Space]

    [SerializeField][Min(0)] private int _ghostSpawnAfterTime = 5;
    [SerializeField][Min(0)] private int _ghostPrepareTime = 60;
    [SerializeField][Min(0)] private int _mainGameTime = 900;

    [Space]

    private NetworkTimer _ghostSpawnTimer;
    private NetworkTimer _ghostPrepareTimer;
    private NetworkTimer _investigatorSpawnTimer;
    private NetworkTimer _mainTimer;

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

        _ = StartCoroutine(ServerInitializeAndStartGame());
    }

    [Server]
    public IEnumerator ServerInitializeAndStartGame()
    {
        if (!isServer)
            yield break;

        yield return new WaitForSeconds(2f);

        var worldGenerator = Instantiate(LobbyNetworkManager.Instance.WorldGeneratorPrefab);
        NetworkServer.Spawn(worldGenerator);
        yield return new WaitForSeconds(0.2f);

        if (LobbyNetworkManager.Instance.DEBUG_MODE)
        {
            _ghostSpawnAfterTime = 5;
            _ghostPrepareTime = 10;
        }

        _ghostSpawnTimer = ServerSpawnNetworkTimer();
        yield return new WaitForSeconds(0.2f);
        _ghostPrepareTimer = ServerSpawnNetworkTimer();
        yield return new WaitForSeconds(0.2f);
        _investigatorSpawnTimer = ServerSpawnNetworkTimer();
        yield return new WaitForSeconds(0.2f);
        _mainTimer = ServerSpawnNetworkTimer();
        yield return new WaitForSeconds(0.2f);

        _ = ServerSpawnNetworkMusicController();
        yield return new WaitForSeconds(0.2f);

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

        yield return new WaitForSeconds(0.2f);

        RpcPlayerRolesAssigned();
        yield return new WaitForSeconds(0.2f);

        ServerStartWorldGeneration();
    }

    [Server]
    public NetworkTimer ServerSpawnNetworkTimer()
    {
        if (!isServer)
            return null;        

        var timerObj = Instantiate(LobbyNetworkManager.Instance.NetworkTimerPrefab);
        NetworkServer.Spawn(timerObj);
        return timerObj.GetComponent<NetworkTimer>();
    }

    [Server]
    public NetworkMusicController ServerSpawnNetworkMusicController()
    {
        if (!isServer)
            return null;

        var networkMusicControllerObj = Instantiate(LobbyNetworkManager.Instance.NetworkMusicControllerPrefab);
        NetworkServer.Spawn(networkMusicControllerObj);
        return networkMusicControllerObj.GetComponent<NetworkMusicController>();
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
        ServerStartPlayerSpawnTimers();
    }

    [ClientRpc]
    public void RpcWorldGenerationCompleted()
    {
        OnClientWorldGenerationCompleted.Invoke();
    }

    [Server]
    public void ServerStartPlayerSpawnTimers()
    {
        if (!isServer)
            return;

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers)
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

        _ghostSpawnTimer.OnTimeRanOut.AddListener(ServerStartGhostPreparePhaseCoroutine);
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
        //timer.OnTimeRanOut.RemoveListener(() => ClientUnsubscribeFromTimer(timer));
    }

    [Server]
    public void ServerStartGhostPreparePhaseCoroutine()
    {
        if (!isServer)
            return;

        RpcGhostPreparePhaseStarted();

        _ = StartCoroutine(ServerStartGhostPreparePhase());
    }

    [Server]
    public IEnumerator ServerStartGhostPreparePhase()
    {
        if (!isServer)
            yield break;

        _ = StartCoroutine(ServerOpenUnlockedDoors());

        yield return new WaitForSeconds(0.1f);

        var mainHallRoom = FindObjectsByType<Room>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r.Type is RoomType.MainHall);

        LobbyNetworkManager.Instance.ServerSpawnGhostPlayer(mainHallRoom.PlayerSpawnPosition, mainHallRoom.transform.rotation);

        yield return new WaitForSeconds(0.1f);

        var ghost = FindAnyObjectByType<GhostPlayerMovement>();
        ghost.CanDash = false;
        ghost.IsInPreparePhase = true;

        yield return new WaitForSeconds(0.1f);

        var ghostAbilities = ghost.GetComponents<GhostAbility>();

        foreach (var ability in ghostAbilities)
            ability.CanBeActivated = false;

        yield return new WaitForSeconds(0.1f);

        OnServerGhostPreparePhaseStarted.Invoke();
        RpcGhostPreparePhaseStarted();

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers)
        {
            var role = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn).Role;

            if (role is not PlayerRole.Ghost)
                continue;

            TargetRpcSubscribeToTimer(conn, _ghostPrepareTimer.netId);
        }

        yield return new WaitForSeconds(0.1f);

        _ghostPrepareTimer.OnTimeRanOut.AddListener(ServerStartMainPhaseCoroutine);
        _ghostPrepareTimer.ServerStartTimer(_ghostPrepareTime);
    }

    [Server]
    public IEnumerator ServerOpenUnlockedDoors()
    {
        if (!isServer)
            yield break;

        var unlockedDoors = FindObjectsByType<Door>(FindObjectsSortMode.None)
            .Where(door => !door.IsLocked);

        foreach (var door in unlockedDoors)
        {
            door.ServerOpen();
            yield return new WaitForSeconds(0.1f);
        }
    }

    [ClientRpc]
    public void RpcGhostPreparePhaseStarted()
    {
        OnClientGhostPreparePhaseStarted.Invoke();
    }

    [Server]
    private void ServerStartMainPhaseCoroutine()
    {
        if (!isServer)
            return;

        _ = StartCoroutine(ServerStartMainPhase());
    }

    [Server]
    public IEnumerator ServerStartMainPhase()
    {
        if (!isServer)
            yield break;

        _ = StartCoroutine(ServerCloseOpenedDoors());

        yield return new WaitForSeconds(0.1f);

        var ghost = FindAnyObjectByType<GhostPlayerMovement>();
        ghost.CanDash = true;
        ghost.IsInPreparePhase = false;

        yield return new WaitForSeconds(0.1f);

        var ghostAbilities = ghost.GetComponents<GhostAbility>();

        foreach (var ability in ghostAbilities)
            ability.CanBeActivated = true;

        yield return new WaitForSeconds(0.1f);

        var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

        if (rooms.Count() > 2)
        {
            var roomToTeleportGhostTo = rooms
                .Where(r => r.Type is not RoomType.MainHall)
                .Where(r => r.LinkedNetworkRoom.Neighbors.None(n => n.Type is RoomType.MainHall))
                .UnityRandomItem();

            ghost.ServerForceTeleportTo(roomToTeleportGhostTo.PlayerSpawnPosition);
        }

        yield return new WaitForSeconds(0.1f);

        var mainHallRoom = FindObjectsByType<Room>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r.Type is RoomType.MainHall);

        LobbyNetworkManager.Instance.ServerSpawnInvestigatorPlayer(mainHallRoom.PlayerSpawnPosition, mainHallRoom.transform.rotation);

        yield return new WaitForSeconds(0.1f);

        OnServerMainPhaseStarted.Invoke();
        RpcMainPhaseStarted();

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers)
            TargetRpcSubscribeToTimer(conn, _mainTimer.netId);

        yield return new WaitForSeconds(0.1f);

        _mainTimer.OnTimeRanOut.AddListener(ServerGhostWin);
        _mainTimer.ServerStartTimer(_mainGameTime);

        _gameStarted = true;
    }

    [Server]
    public IEnumerator ServerCloseOpenedDoors()
    {
        if (!isServer)
            yield break;

        var openedDoors = FindObjectsByType<Door>(FindObjectsSortMode.None)
            .Where(door => door.IsOpened);

        foreach (var door in openedDoors)
        {
            door.ServerClose();
            yield return new WaitForSeconds(0.1f);
        }
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

        OnServerGameOver.Invoke();

        _mainTimer.ServerStopTimer();

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers)
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

        OnServerGameOver.Invoke();

        foreach (var conn in LobbyNetworkManager.Instance.ConnectedPlayers)
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
