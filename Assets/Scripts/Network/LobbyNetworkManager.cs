using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Utp;

public class LobbyNetworkManager : NetworkManager
{
    public static LobbyNetworkManager Instance;

    [Header("DEBUGGING")]

    public bool DEBUG_MODE = false;

    [Header("Network Manager Events")]

    public static UnityEvent OnServerSceneChangedAndLoaded = new();
    public static UnityEvent OnClientSceneChangedAndLoaded = new();
    public static UnityEvent OnServerAllPlayersReady = new();
    public static UnityEvent OnClientSceneReady = new();
    public static UnityEvent OnClientConnected = new();
    public static UnityEvent OnClientDisconnected = new();

    [Header("Scenes")]

    [Scene][SerializeField] private string _menuScene;
    [Scene][SerializeField] private string _lobbyScene;
    [Scene][SerializeField] private string _gameScene;

    [Header("Spawnable Prefabs")]

    [SerializeField] private GameObject _lobbyNotifierPrefab;
    [SerializeField] private GameObject _playerDataPrefab;
    [SerializeField] private GameObject _lobbyPlayerPrefab;
    [SerializeField] private GameObject _ghostPlayerPrefab;
    [SerializeField] private GameObject _investigatorPlayerPrefab;
    [SerializeField] private GameObject _gameManagerPrefab;
    [SerializeField] private GameObject _worldGeneratorPrefab;
    [SerializeField] private GameObject _networkTimerPrefab;

    [Space]

    [SerializeField] private GameObject _mainHallPrefab;
    [SerializeField] private GameObject _emergencyExitPrefab;
    [SerializeField] private GameObject _hallPrefab;
    [SerializeField] private GameObject _livingRoomPrefab;
    [SerializeField] private GameObject _simpleCorridorPrefab;
    [SerializeField] private GameObject _tShapedCorridorPrefab;
    [SerializeField] private GameObject _stairwayRoomPrefab;
    [SerializeField] private GameObject _statuesPuzzleRoomPrefab;
    [SerializeField] private GameObject _rotatingMirrorsPuzzleRoomPrefab;

    [Space]

    [SerializeField] private GameObject _candlesticksPuzzlePrefab;
    [SerializeField] private GameObject _statuesPuzzlePrefab;
    [SerializeField] private GameObject _rotatingMirrorsPuzzlePrefab;
    [SerializeField] private GameObject _investigatorWinTriggerPrefab;

    [Space]

    [SerializeField] private GameObject _candlestickPrefab;
    [SerializeField] private GameObject _statuePrefab;
    [SerializeField] private GameObject _windRosePrefab;
    [SerializeField] private GameObject _rotatingMirrorPrefab;
    [SerializeField] private GameObject _reflectableLightSourcePrefab;
    [SerializeField] private GameObject _reflectableLightTargetPrefab;

    [Space]

    [SerializeField] private GameObject _amuletPrefab;
    [SerializeField] private GameObject _doorPrefab;
    [SerializeField] private GameObject _notePrefab;

    [Space]

    [SerializeField] private GameObject _timeCatcherPrefab;

    public GameObject WorldGeneratorPrefab => _worldGeneratorPrefab;
    public GameObject NetworkTimerPrefab => _networkTimerPrefab;

    public GameObject MainHallPrefab => _mainHallPrefab;
    public GameObject EmergencyExitPrefab => _emergencyExitPrefab;
    public GameObject HallPrefab => _hallPrefab;
    public GameObject LivingRoomPrefab => _livingRoomPrefab;
    public GameObject SimpleCorridorPrefab => _simpleCorridorPrefab;
    public GameObject TShapedCorridorPrefab => _tShapedCorridorPrefab;
    public GameObject StairwayRoomPrefab => _stairwayRoomPrefab;
    public GameObject StatuesPuzzleRoomPrefab => _statuesPuzzleRoomPrefab;
    public GameObject RotatingMirrorsPuzzleRoomPrefab => _rotatingMirrorsPuzzleRoomPrefab;

    public GameObject CandlesticksPuzzlePrefab => _candlesticksPuzzlePrefab;
    public GameObject StatuesPuzzlePrefab => _statuesPuzzlePrefab;
    public GameObject RotatingMirrorsPuzzlePrefab => _rotatingMirrorsPuzzlePrefab;
    public GameObject InvestigatorWinTriggerPrefab => _investigatorWinTriggerPrefab;

    public GameObject CandlestickPrefab => _candlestickPrefab;
    public GameObject StatuePrefab => _statuePrefab;
    public GameObject WindRosePrefab => _windRosePrefab;
    public GameObject RotatingMirrorPrefab => _rotatingMirrorPrefab;
    public GameObject ReflectableLightSourcePrefab => _reflectableLightSourcePrefab;
    public GameObject ReflectableLightTargetPrefab => _reflectableLightTargetPrefab;

    public GameObject AmuletPrefab => _amuletPrefab;
    public GameObject DoorPrefab => _doorPrefab;
    public GameObject NotePrefab => _notePrefab;

    public GameObject TimeCatcherPrefab => _timeCatcherPrefab;

    private UtpTransport _utpTransport;
    public ushort Port => _utpTransport.Port;

    private readonly Dictionary<NetworkConnectionToClient, PlayerData> _connectedPlayersData = new();
    public List<NetworkConnectionToClient> ConnectedPlayers => _connectedPlayersData.Keys.ToList();
    public List<PlayerData> ConnectedPlayersData => _connectedPlayersData.Values.ToList();
    public PlayerData GetConnectedPlayerData(NetworkConnectionToClient connectionToClient) =>
        _connectedPlayersData.TryGetValue(connectionToClient, out PlayerData playerData) ? playerData : null;
    public NetworkConnectionToClient GetConnectionForRole(PlayerRole role) =>
        _connectedPlayersData.FirstOrDefault(kvPair => kvPair.Value.Role == role).Key;

    private readonly List<NetworkConnectionToClient> _readyConnections = new();

    public override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        _utpTransport = GetComponent<UtpTransport>();
    }

    #region Network Role Management

    /// <summary>
    /// Ensures Relay is disabled. Starts the server, listening for incoming connections.
    /// </summary>
    public void StartStandardServer()
    {
        _utpTransport.useRelay = false;
        StartServer();
    }

    /// <summary>
    /// Ensures Relay is disabled. Starts a network "host" - a server and a client in the same application.
    /// </summary>
    public void StartStandardHost()
    {
        _utpTransport.useRelay = false;
        StartHost();
    }

    /// <summary>
    /// Ensures Relay is disabled. Starts the client, connects it to the server with <paramref name="address"/>.
    /// </summary>
    /// <param name="address">
    /// The server address to connect to.
    /// </param>
    public void JoinStandardServer(string address)
    {
        _utpTransport.useRelay = false;
        networkAddress = address;
        StartClient();
    }

    /// <summary>
    /// Gets available Relay regions.
    /// </summary>
    public void GetRelayRegions(Action<List<Region>> onSuccess, Action onFailure)
    {
        _utpTransport.GetRelayRegions(onSuccess, onFailure);
    }

    /// <summary>
    /// Ensures Relay is enabled. Starts a network "host" - a server and a client in the same application.
    /// Connects to the Relay server using a <see cref="RelayServerData"/> object.
    /// </summary>
    /// <param name="relayServerData">
    /// The Relay server data.
    /// </param>
    public void StartRelayHost(RelayServerData relayServerData)
    {
        _utpTransport.useRelay = true;
        _utpTransport.SetRelayServerData(relayServerData);
        StartHost();
    }

    /// <summary>
    /// Ensures Relay is enabled. Starts the client, connects to the Relay server using a <see cref="RelayServerData"/> object.
    /// </summary>
    /// <param name="relayServerData">
    /// The Relay server data.
    /// </param>
    public void JoinRelayServer(RelayServerData relayServerData)
    {
        _utpTransport.useRelay = true;
        _utpTransport.SetRelayServerData(relayServerData);
        StartClient();
    }

    #endregion Network Role Management

    #region Player Spawning

    [Server]
    public void ServerSpawnLobbyPlayer(NetworkConnectionToClient conn)
    {
        if (!NetworkServer.active)
            return;

        Debug.Log($"Spawning lobby player for connection {conn.connectionId}");

        if (!_connectedPlayersData.ContainsKey(conn))
        {
            Debug.LogError("Attempted to spawn Lobby Player for an unregistered connection.");
            conn.Disconnect();
            return;
        }

        if (_lobbyPlayerPrefab == null)
        {
            Debug.LogError("The Lobby Player Prefab is empty on the Network Manager. Please setup a Lobby Player Prefab object.");
            return;
        }

        if (!_lobbyPlayerPrefab.TryGetComponent(out NetworkIdentity _))
        {
            Debug.LogError("The Lobby Player Prefab does not have a Network Identity. Please add a Network Identity to the Lobby Player Prefab.");
            return;
        }

        if (!_lobbyPlayerPrefab.TryGetComponent(out LobbyPlayer _))
        {
            Debug.LogError("The Lobby Player Prefab does not have a Lobby Player component. Please add a Lobby Player component to the Lobby Player Prefab.");
            return;
        }

        NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Destroy);

        GameObject lobbyPlayer = Instantiate(_lobbyPlayerPrefab);

        _ = NetworkServer.AddPlayerForConnection(conn, lobbyPlayer);

        bool allLobbyPlayersAreSpawned = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None).Length == _connectedPlayersData.Count;

        if (allLobbyPlayersAreSpawned)
            LobbyNotifier.Instance.RpcAllLobbyPlayersSpawned();
    }

    [Server]
    public void ServerSpawnGhostPlayer()
    {
        if (!NetworkServer.active)
            return;

        var conn = GetConnectionForRole(PlayerRole.Ghost);

        if (conn == null)
        {
            Debug.LogError("Attempted to spawn Ghost Player with no players with an assigned Ghost role.");
            return;
        }

        if (_ghostPlayerPrefab == null)
        {
            Debug.LogError($"The Ghost Player Prefab is empty on the Network Manager. Please setup a Ghost Player Prefab object.");
            return;
        }

        if (!_ghostPlayerPrefab.TryGetComponent(out NetworkIdentity _))
        {
            Debug.LogError($"The Ghost Player Prefab does not have a Network Identity. Please add a Network Identity to the Ghost Player Prefab.");
            return;
        }

        if (!_ghostPlayerPrefab.TryGetComponent(out GhostPlayerMovement _))
        {
            Debug.LogError($"The Ghost Player Prefab does not have a Ghost Player Movement component. Please add a Ghost Player Movement component to the Ghost Player Prefab.");
            return;
        }

        GameObject ghostPlayer = Instantiate(_ghostPlayerPrefab);

        _ = NetworkServer.ReplacePlayerForConnection(conn, ghostPlayer, ReplacePlayerOptions.Destroy);
    }

    [Server]
    public void ServerSpawnInvestigatorPlayer()
    {
        if (!NetworkServer.active)
            return;

        var conn = GetConnectionForRole(PlayerRole.Investigator);

        if (conn == null)
        {
            Debug.LogError("Attempted to spawn Investigator Player with no players with an assigned Investigator role.");
            return;
        }

        if (_investigatorPlayerPrefab == null)
        {
            Debug.LogError($"The Investigator Player Prefab is empty on the Network Manager. Please setup an Investigator Player Prefab object.");
            return;
        }

        if (!_investigatorPlayerPrefab.TryGetComponent(out NetworkIdentity _))
        {
            Debug.LogError($"The Investigator Player Prefab does not have a Network Identity. Please add a Network Identity to the Investigator Player Prefab.");
            return;
        }

        if (!_investigatorPlayerPrefab.TryGetComponent(out InvestigatorPlayerMovement _))
        {
            Debug.LogError($"The Investigator Player Prefab does not have a Investigator Player Movement component. Please add an Investigator Player Movement component to the Investigator Player Prefab.");
            return;
        }

        GameObject investigatorPlayer = Instantiate(_investigatorPlayerPrefab);

        _ = NetworkServer.ReplacePlayerForConnection(conn, investigatorPlayer, ReplacePlayerOptions.Destroy);
    }

    #endregion Player Spawning

    #region Lobby Management

    [Server]
    public void ServerCheckAllPlayersReady()
    {
        if (!NetworkServer.active)
            return;

        bool allLobbyPlayersAreReady = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None)
            .All(lobbyPlayer => lobbyPlayer.IsReady);

        if (allLobbyPlayersAreReady)
            GoToGameScene();
    }

    #endregion Lobby Management

    #region Server Scene Management

    public void GoToMenuScene()
    {
        if (NetworkServer.active)
            ServerChangeSceneByPath(_menuScene);
        else
            _ = SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(_menuScene));
    }

    [Server]
    public void GoToLobbyScene() => ServerChangeSceneByPath(_lobbyScene);

    [Server]
    public void GoToGameScene() => ServerChangeSceneByPath(_gameScene);

    [Server]
    public void ServerChangeSceneByPath(string path) => ServerChangeScene(System.IO.Path.GetFileNameWithoutExtension(path));

    [Server]
    private void ServerOnAllPlayersReadyOnMenuScene()
    {
        if (!NetworkServer.active)
            return;

        GameObject lobbyNotifier = Instantiate(_lobbyNotifierPrefab);
        NetworkServer.Spawn(lobbyNotifier);

        foreach (var connection in _readyConnections)
        {
            if (_playerDataPrefab == null)
            {
                Debug.LogError($"The Player Data Prefab is empty on the Network Manager. Please setup a Player Data Prefab object.");
                return;
            }

            if (!_playerDataPrefab.TryGetComponent(out NetworkIdentity _))
            {
                Debug.LogError($"The Player Data Prefab does not have a Network Identity. Please add a Network Identity to the Player Data Prefab.");
                return;
            }

            if (!_playerDataPrefab.TryGetComponent(out PlayerData _))
            {
                Debug.LogError($"The Player Data Prefab does not have a Player Data component. Please add a Player Data component to the Player Data Prefab.");
                return;
            }

            var playerData = Instantiate(_playerDataPrefab);
            NetworkServer.Spawn(playerData);
            _ = playerData.GetComponent<NetworkIdentity>().AssignClientAuthority(connection);

            _connectedPlayersData[connection] = playerData.GetComponent<PlayerData>();
        };

        GoToLobbyScene();
    }

    [Server]
    private void ServerOnAllPlayersReadyOnGameScene()
    {
        if (!NetworkServer.active)
            return;

        var gameManager = Instantiate(_gameManagerPrefab);
        NetworkServer.Spawn(gameManager);

        LobbyNotifier.Instance.RpcSceneReady();
    }

    #endregion Server Scene Management

    #region Server Overrides

    [Server]
    public override void OnStartServer()
    {
        // We probably shouldn't register them here because in this game a "server" is actually a host.
        // spawnPrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs").ToList();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (NetworkServer.connections.Count > maxConnections || SceneManager.GetActiveScene().path != _menuScene)
        {
            conn.Disconnect();
            return;
        }

        _connectedPlayersData.Add(conn, null);
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        _readyConnections.Add(conn);

        if (_readyConnections.Count < maxConnections)
            return;

        OnServerAllPlayersReady.Invoke();

        string scenePath = SceneManager.GetActiveScene().path;

        if (scenePath.Equals(_menuScene))
        {
            ServerOnAllPlayersReadyOnMenuScene();
        }
        else if (scenePath.Equals(_gameScene))
        {
            ServerOnAllPlayersReadyOnGameScene();
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (!SceneManager.GetActiveScene().path.Equals(_lobbyScene))
            return;

        ServerSpawnLobbyPlayer(conn);
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        _readyConnections.Clear();
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        OnServerSceneChangedAndLoaded.Invoke();

        // Debug.LogError($"[Server] Scene changed. Network objects: {string.Join(", ", FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None).Select(obj => $"{obj.name} (netId={obj.netId})"))}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log("on server disconnect");

        if (!_connectedPlayersData.ContainsKey(conn))
            return;

        _connectedPlayersData[conn].Role = PlayerRole.None;
        _connectedPlayersData[conn] = null;

        _ = _connectedPlayersData.Remove(conn);

        NetworkServer.DestroyPlayerForConnection(conn);

        // Since the server is the host, we need to have 1 leftover player in order to shut down, not 0 players.
        if (_connectedPlayersData.Count <= 1)
        {
            ServerStopSessionAsync();

            _readyConnections.Clear();
            _connectedPlayersData.Clear();

            if (!SceneManager.GetActiveScene().path.Equals(_menuScene))
                GoToMenuScene();
        }
    }

    [Server]
    public async void ServerStopSessionAsync()
    {
        if (!NetworkServer.active)
            return;

        StopSessionStatus stopSessionStatus = await SessionManager.Instance.StopSession();

        if (stopSessionStatus is StopSessionStatus.Failed)
            _ = StartCoroutine(ServerTryStopSessionAfterDelay(1f));
    }

    [Server]
    private IEnumerator ServerTryStopSessionAfterDelay(float delay)
    {
        if (!NetworkServer.active)
            yield break;

        yield return new WaitForSeconds(delay);
        ServerStopSessionAsync();
    }

    #endregion Server Overrides

    #region Client Overrides

    [Client]
    public override void OnStartClient()
    {
        if (!NetworkClient.active)
            return;

        /*
        var spawnablePrefabs = Resources.LoadAll<GameObject>("SpawnablePrefabs");

        foreach (var prefab in spawnablePrefabs)
            NetworkClient.RegisterPrefab(prefab);
        */

        _ = _lobbyNotifierPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _playerDataPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _lobbyPlayerPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _ghostPlayerPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _investigatorPlayerPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _gameManagerPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _worldGeneratorPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _networkTimerPrefab.Bind(NetworkClient.RegisterPrefab);

        _ = _mainHallPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _emergencyExitPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _hallPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _livingRoomPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _simpleCorridorPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _tShapedCorridorPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _stairwayRoomPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _statuesPuzzleRoomPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _rotatingMirrorsPuzzleRoomPrefab.Bind(NetworkClient.RegisterPrefab);

        _ = _candlesticksPuzzlePrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _statuesPuzzlePrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _rotatingMirrorsPuzzlePrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _investigatorWinTriggerPrefab.Bind(NetworkClient.RegisterPrefab);

        _ = _candlestickPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _statuePrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _windRosePrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _rotatingMirrorPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _reflectableLightSourcePrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _reflectableLightTargetPrefab.Bind(NetworkClient.RegisterPrefab);

        _ = _amuletPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _doorPrefab.Bind(NetworkClient.RegisterPrefab);
        _ = _notePrefab.Bind(NetworkClient.RegisterPrefab);

        _ = _timeCatcherPrefab.Bind(NetworkClient.RegisterPrefab);
    }

    public override void OnClientConnect()
    {
        if (!clientLoadedScene)
        {
            if (!NetworkClient.ready)
                _ = NetworkClient.Ready();
        }

        OnClientConnected.Invoke();
    }

    public override void OnClientSceneChanged()
    {
        OnClientSceneChangedAndLoaded.Invoke();

        if (NetworkClient.connection.isAuthenticated && !NetworkClient.ready)
            _ = NetworkClient.Ready();

        if (SceneManager.GetActiveScene().path.Equals(_lobbyScene))
            _ = NetworkClient.AddPlayer();

        // Debug.LogError($"[Client] Scene changed. Network objects: {string.Join(", ", FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None).Select(obj => $"{obj.name} (netId={obj.netId})"))}");
    }

    public override void OnClientDisconnect()
    {
        OnClientDisconnected.Invoke();

        if (!SceneManager.GetActiveScene().path.Equals(_menuScene))
            GoToMenuScene();
    }

    #endregion Client Overrides
}
