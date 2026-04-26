using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance;

    public static UnityEvent<float> OnGenerationProgressUpdated = new();
    public static UnityEvent<float> OnReconstructionProgressUpdated = new();
    public static UnityEvent OnGenerationCompleted = new();
    //public static UnityEvent<NetworkConnectionToClient, bool> OnReconstructionCompleted = new();

    [SerializeField] private int _seed = 1;

    /*
    // TODO: Make WorldData serializable !!!
    public object WorldData { get; private set; }
    public string WorldHash { get; private set; }
    */

    private readonly List<Room> _spawnedRooms = new();
    private readonly List<Puzzle> _spawnedPuzzles = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    [Server]
    public void ServerSetSeed(int seed)
    {
        if (!isServer)
            return;

        _seed = seed;
    }

    [Server]
    public void ServerGenerateWorld()
    {
        if (!isServer)
            return;

        _ = StartCoroutine(ServerGenerateWorldCoroutine());
    }

    [Server]
    public IEnumerator ServerGenerateWorldCoroutine()
    {
        if (!isServer)
            yield break;

        _spawnedRooms.Clear();
        _spawnedPuzzles.Clear();

        var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None)
            .Where(sp => sp.Type is SpawnPointType.Room or
                SpawnPointType.Puzzle or
                SpawnPointType.Amulet or
                SpawnPointType.Door or
                SpawnPointType.Note or
                SpawnPointType.PuzzleDoor or
                SpawnPointType.InvestigatorWinTrigger)
            .ToList();

        int spawnPointCount = spawnPoints.Count;
        int roomSpawnPointCount = spawnPoints.Count(sp => sp.Type is SpawnPointType.Room);
        int puzzleSpawnPointCount = spawnPoints.Count(sp => sp.Type is SpawnPointType.Puzzle);
        int totalActions = spawnPointCount + roomSpawnPointCount + puzzleSpawnPointCount;
        int completedActions = 0;

        Random.State savedState = Random.state;
        Random.InitState(_seed);

        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f || spawnPoint.Type is SpawnPointType.Empty)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            switch (spawnPoint.Type)
            {
                case SpawnPointType.Room:
                    if (spawnPoint.TryGetComponent(out RoomSpawnPoint roomSpawnPoint))
                    {
                        var room = ServerSpawnRoom(roomSpawnPoint.RoomType, spawnPoint.transform.position, spawnPoint.transform.rotation);
                        _spawnedRooms.Add(room);
                    }
                    else
                    {
                        Debug.LogWarning($"Spawn Point {spawnPoint.gameObject.name} with type Room is not a Room Spawn Point. The spawn point will be ignored.");
                    }
                    break;

                case SpawnPointType.Puzzle:
                    if (spawnPoint.TryGetComponent(out PuzzleSpawnPoint puzzleSpawnPoint))
                    {
                        var puzzle = ServerSpawnPuzzle(puzzleSpawnPoint.PuzzleType, spawnPoint.transform.position, spawnPoint.transform.rotation);
                        _spawnedPuzzles.Add(puzzle);
                    }
                    else
                    {
                        Debug.LogWarning($"Spawn Point {spawnPoint.gameObject.name} with type Puzzle is not a Puzzle Spawn Point. The spawn point will be ignored.");
                    }
                    break;

                case SpawnPointType.Amulet:
                    _ = ServerSpawnAmulet(spawnPoint.transform.position, spawnPoint.transform.rotation);
                    break;

                case SpawnPointType.Door:
                    _ = ServerSpawnDoor(spawnPoint.transform.position, spawnPoint.transform.rotation);
                    break;

                case SpawnPointType.Note:
                    _ = ServerSpawnNote(spawnPoint.transform.position, spawnPoint.transform.rotation);
                    break;

                case SpawnPointType.PuzzleDoor:
                    var door = ServerSpawnDoor(spawnPoint.transform.position, spawnPoint.transform.rotation);
                    door.ServerLock();
                    break;

                case SpawnPointType.InvestigatorWinTrigger:
                    _ = ServerSpawnInvestigatorWinTrigger(spawnPoint.transform.position, spawnPoint.transform.rotation);
                    break;
            }

            completedActions++;
            OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

            yield return new WaitForSeconds(0.1f);
        }

        foreach (var room in _spawnedRooms)
        {
            room.ServerInitialize();

            completedActions++;
            OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

            yield return new WaitForSeconds(0.1f);
        }

        foreach (var puzzle in _spawnedPuzzles)
        {
            puzzle.ServerInitialize();

            completedActions++;
            OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

            yield return new WaitForSeconds(0.1f);
        }

        OnGenerationProgressUpdated.Invoke(1f);

        yield return new WaitForSeconds(0.1f);

        Random.state = savedState;

        OnGenerationCompleted.Invoke();

        /*
        for (int i = 0; i < 5; i++)
        {
            OnGenerationProgressUpdated.Invoke(i / 5f);
            yield return new WaitForSeconds(0.1f);
        }

        OnGenerationProgressUpdated.Invoke(1f);
        yield return new WaitForSeconds(1f);

        WorldData = (First: 1f, Second: 2f, Third: 3f);
        WorldHash = ComputeWorldHash(WorldData);

        OnGenerationCompleted.Invoke();
        */
    }

    [Server]
    public Amulet ServerSpawnAmulet(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.AmuletPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Amulet is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Amulet>();
    }

    [Server]
    public Candlestick ServerSpawnCandlestick(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.CandlestickPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Candlestick is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Candlestick>();
    }

    [Server]
    public Door ServerSpawnDoor(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.DoorPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Door is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponentInChildren<Door>();
    }

    [Server]
    public InvestigatorWinTrigger ServerSpawnInvestigatorWinTrigger(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.InvestigatorWinTriggerPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Investigator Win Trigger is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<InvestigatorWinTrigger>();
    }

    [Server]
    public Note ServerSpawnNote(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.NotePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Note is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);

        var note = gameObj.GetComponent<Note>();
        note.ServerSetLoreText();

        return note;
    }

    [Server]
    public Puzzle ServerSpawnPuzzle(PuzzleType type, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = type switch
        {
            PuzzleType.Candlesticks => LobbyNetworkManager.Instance.CandlesticksPuzzlePrefab,
            PuzzleType.Statues => LobbyNetworkManager.Instance.StatuesPuzzlePrefab,
            PuzzleType.RotatingMirrors => LobbyNetworkManager.Instance.RotatingMirrorsPuzzlePrefab,
            _ => throw new System.InvalidOperationException("Unsupported puzzle type encountered while spawning puzzles. Make sure all puzzle spawn points are set up properly.")
        };

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Puzzle with type {type} is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);

        var puzzle = gameObj.GetComponent<Puzzle>();
        puzzle.ServerSetType(type);

        return puzzle;
    }

    [Server]
    public ReflectableLightSource ServerSpawnReflectableLightSource(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.ReflectableLightSourcePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Reflectable Light Source is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<ReflectableLightSource>();
    }

    [Server]
    public ReflectableLightTarget ServerSpawnReflectableLightTarget(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.ReflectableLightTargetPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Reflectable Light Target is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<ReflectableLightTarget>();
    }

    [Server]
    public Room ServerSpawnRoom(RoomType type, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = type switch
        {
            RoomType.MainHall => LobbyNetworkManager.Instance.MainHallPrefab,
            RoomType.EmergencyExit => LobbyNetworkManager.Instance.EmergencyExitPrefab,
            RoomType.Hall => LobbyNetworkManager.Instance.HallPrefab,
            RoomType.LivingRoom => LobbyNetworkManager.Instance.LivingRoomPrefab,
            RoomType.SimpleCorridor => LobbyNetworkManager.Instance.SimpleCorridorPrefab,
            RoomType.TShapedCorridor => LobbyNetworkManager.Instance.TShapedCorridorPrefab,
            RoomType.Stairway => LobbyNetworkManager.Instance.StairwayRoomPrefab,
            RoomType.StatuesPuzzleRoom => LobbyNetworkManager.Instance.StatuesPuzzleRoomPrefab,
            RoomType.RotatingMirrorsPuzzleRoom => LobbyNetworkManager.Instance.RotatingMirrorsPuzzleRoomPrefab,
            _ => throw new System.InvalidOperationException("Unsupported room type encountered while spawning rooms. Make sure all room spawn points are set up properly.")
        };

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Room with type {type} is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);

        var room = gameObj.GetComponent<Room>();
        room.ServerSetType(type);

        return room;
    }

    [Server]
    public RotatingMirror ServerSpawnRotatingMirror(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.RotatingMirrorPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Rotating Mirror is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<RotatingMirror>();
    }

    [Server]
    public Statue ServerSpawnStatue(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.StatuePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Statue is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Statue>();
    }

    [Server]
    public TimeCatcher ServerSpawnTimeCatcher(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.TimeCatcherPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Time Catcher is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<TimeCatcher>();
    }

    [Server]
    public GameObject ServerSpawnWindRose(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.WindRosePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Wind Rose is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation, parent);
        NetworkServer.Spawn(gameObj);
        return gameObj;
    }

    /*
    [ClientRpc]
    public void RpcReconstructWorld(object worldData, string worldHash)
    {
        // TODO: revert all world generation
        WorldData = null;
        WorldHash = string.Empty;

        _ = StartCoroutine(ClientReconstructWorldCoroutine(worldData, worldHash));
    }

    [TargetRpc]
    public void TargetRpcReconstructWorld(NetworkConnectionToClient conn, object worldData, string worldHash)
    {
        // TODO: revert all world generation
        WorldData = null;
        WorldHash = string.Empty;

        _ = StartCoroutine(ClientReconstructWorldCoroutine(worldData, worldHash));
    }

    [Client]
    public IEnumerator ClientReconstructWorldCoroutine(object worldData, string worldHash)
    {
        if (!isClient)
            yield break;

        string localHash = ComputeWorldHash(worldData);

        if (!localHash.Equals(worldHash))
        {
            CmdSetWorldReconstructionResult(false);
            yield break;
        }

        // TODO: reconstruct the world

        for (int i = 0; i < 5; i++)
        {
            OnReconstructionProgressUpdated.Invoke(i / 5f);
            yield return new WaitForSeconds(0.1f);
        }

        OnReconstructionProgressUpdated.Invoke(1f);
        yield return new WaitForSeconds(1f);

        WorldData = (First: 1f, Second: 2f, Third: 3f);
        WorldHash = ComputeWorldHash(WorldData);

        CmdSetWorldReconstructionResult(true);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetWorldReconstructionResult(bool success, NetworkConnectionToClient conn = null)
    {
        OnReconstructionCompleted.Invoke(conn, success);
    }

    private static string ComputeWorldHash(object worldData)
    {
        long hash = 0;

        // TODO: compute world hash
        hash += GetHash(worldData);

        return hash.ToString();
    }

    private static long GetHash<T>(T value)
    {
        if (value == null)
            return 0L;

        return value switch
        {
            int i => i,
            long l => l,
            Enum e => GetHash(Convert.ToInt64(e)),
            Vector2Int v2i => v2i.x + v2i.y,
            Vector3Int v3i => v3i.x + v3i.y + v3i.z,
            Vector3Long v3l => v3l.x + v3l.y + v3l.z,
            Vector4Long v4l => v4l.x + v4l.y + v4l.z + v4l.w,
            float f => BitConverter.SingleToInt32Bits(f), // TODO: take care of different endianness on different platforms
            double d => BitConverter.DoubleToInt64Bits(d), // TODO: take care of different endianness on different platforms
            Vector2 v2 => GetHash(v2.x) + GetHash(v2.y),
            Vector3 v3 => GetHash(v3.x) + GetHash(v3.y) + GetHash(v3.z),
            Vector4 v4 => GetHash(v4.x) + GetHash(v4.y) + GetHash(v4.z) + GetHash(v4.w),
            Quaternion q => GetHash(q.x) + GetHash(q.y) + GetHash(q.z) + GetHash(q.w),
            char c => c,
            string s => s.ToCharArray().Select(ch => GetHash(ch)).Sum(),
            bool b => b ? 1L : 0L,
            _ => 0L
        };
    }
    */
}
