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

    private readonly Dictionary<Room, NetworkRoom> _spawnedNetworkRooms = new();

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

        yield return new WaitForSeconds(1f);

        int totalActions = 14;
        int completedActions = 0;

        int seed = _seed == 0 ? Random.Range(0, 10000) : _seed;

        Random.State savedState = Random.state;
        Random.InitState(seed);

        _spawnedNetworkRooms.Clear();

        // 1. Find spawnpoints for NetworkRoom, InvestigatorWinTrigger, Door, PuzzleDoor

        var networkRoomSpawnPoints = FindObjectsByType<NetworkRoomSpawnPoint>(FindObjectsSortMode.None);
        var investigatorWinTriggerSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(sp => sp.Type is SpawnPointType.InvestigatorWinTrigger).ToList();
        var doorSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(sp => sp.Type is SpawnPointType.Door).ToList();
        var puzzleDoorSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(sp => sp.Type is SpawnPointType.PuzzleDoor).ToList();

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 2. Spawn network rooms

        foreach (var spawnPoint in networkRoomSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            var networkRoom = ServerSpawnNetworkRoom(spawnPoint.Room.Type, spawnPoint.transform.position, spawnPoint.transform.rotation);
            _spawnedNetworkRooms[spawnPoint.Room] = networkRoom;

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 3. Spawn investigator win triggers

        foreach (var spawnPoint in investigatorWinTriggerSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            _ = ServerSpawnInvestigatorWinTrigger(spawnPoint.transform.position, spawnPoint.transform.rotation);

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 4. Spawn doors

        foreach (var spawnPoint in doorSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            _ = ServerSpawnDoor(spawnPoint.transform.position, spawnPoint.transform.rotation);

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 5. Spawn puzzle doors

        foreach (var spawnPoint in puzzleDoorSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            var door = ServerSpawnDoor(spawnPoint.transform.position, spawnPoint.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            door.ServerLock();

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 6. Initialize spawned rooms

        foreach (var kvPair in _spawnedNetworkRooms)
        {
            var room = kvPair.Key;
            var networkRoom = kvPair.Value;
            networkRoom.ServerInitialize(room);

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 7. Find spawnpoints for Puzzle

        var puzzleSpawnPoints = FindObjectsByType<PuzzleSpawnPoint>(FindObjectsSortMode.None);

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 8. Spawn and initialize puzzles

        foreach (var spawnPoint in puzzleSpawnPoints)
        {
            var puzzle = ServerSpawnPuzzle(spawnPoint.PuzzleType, spawnPoint.transform.position, spawnPoint.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            yield return puzzle.ServerInitialize();

            // We have to wait longer here because SetText messages of puzzle notes contain texts, which make network messages larger.
            yield return new WaitForSeconds(0.9f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 9. Find spawnpoints for CabinetDoor and Drawer

        var cabinetDoorSpawnPoints = FindObjectsByType<CabinetDoorSpawnPoint>(FindObjectsSortMode.None);
        var drawerSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(sp => sp.Type is SpawnPointType.Drawer);

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 10. Spawn cabinet doors

        foreach (var spawnPoint in cabinetDoorSpawnPoints)
        {
            _ = ServerSpawnCabinetDoor(spawnPoint.IsLeftDoor, spawnPoint.transform.position, spawnPoint.transform.rotation);

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 11. Spawn and initialize drawers

        foreach (var spawnPoint in drawerSpawnPoints)
        {
            var drawer = ServerSpawnDrawer(spawnPoint.transform.position, spawnPoint.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            yield return drawer.ServerInitialize();

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 12. Find spawnpoints for Amulet and Note

        var amuletSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(sp => sp.Type is SpawnPointType.Amulet).ToList();
        var noteSpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Where(sp => sp.Type is SpawnPointType.Note).ToList();

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 13. Spawn amulets

        foreach (var spawnPoint in amuletSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            _ = ServerSpawnAmulet(spawnPoint.transform.position, spawnPoint.transform.rotation);

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(0.3f);

        // 14. Spawn notes

        foreach (var spawnPoint in noteSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            var note = ServerSpawnNote(spawnPoint.transform.position, spawnPoint.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            note.ServerSetLoreText();

            yield return new WaitForSeconds(0.3f);
        }

        completedActions++;
        OnGenerationProgressUpdated.Invoke((float) completedActions / totalActions);

        yield return new WaitForSeconds(1f);

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
    public Amulet ServerSpawnAmulet(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.AmuletPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Amulet is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Amulet>();
    }

    [Server]
    public CabinetDoor ServerSpawnCabinetDoor(bool isLeftDoor, Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = isLeftDoor
            ? LobbyNetworkManager.Instance.LeftCabinetDoorPrefab
            : LobbyNetworkManager.Instance.RightCabinetDoorPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for either Cabinet Door is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<CabinetDoor>();
    }

    [Server]
    public Candlestick ServerSpawnCandlestick(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.CandlestickPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Candlestick is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Candlestick>();
    }

    [Server]
    public Door ServerSpawnDoor(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.DoorPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Door is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponentInChildren<Door>();
    }

    [Server]
    public Drawer ServerSpawnDrawer(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.DrawerPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Drawer is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Drawer>();
    }

    [Server]
    public Holdable ServerSpawnHoldable(HoldableType type, Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = type switch
        {
            HoldableType.Skull => LobbyNetworkManager.Instance.HoldableSkullPrefab,
            HoldableType.Globe => LobbyNetworkManager.Instance.HoldableGlobePrefab,
            HoldableType.Crystal => LobbyNetworkManager.Instance.HoldableCrystalPrefab,
            HoldableType.SandClock => LobbyNetworkManager.Instance.HoldableSandClockPrefab,
            _ => throw new System.InvalidOperationException("Unsupported holdable type encountered while spawning holdables. Make sure all holdable spawn points are set up properly.")
        };

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Holdable with type {type} is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Holdable>();
    }

    [Server]
    public InvestigatorWinTrigger ServerSpawnInvestigatorWinTrigger(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.InvestigatorWinTriggerPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Investigator Win Trigger is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<InvestigatorWinTrigger>();
    }

    [Server]
    public Note ServerSpawnNote(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.NotePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Note is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Note>();
    }

    [Server]
    public Puzzle ServerSpawnPuzzle(PuzzleType type, Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = type switch
        {
            PuzzleType.Candlesticks => LobbyNetworkManager.Instance.CandlesticksPuzzlePrefab,
            PuzzleType.Holdables => LobbyNetworkManager.Instance.HoldablesPuzzlePrefab,
            PuzzleType.Statues => LobbyNetworkManager.Instance.StatuesPuzzlePrefab,
            PuzzleType.RotatingMirrors => LobbyNetworkManager.Instance.RotatingMirrorsPuzzlePrefab,
            PuzzleType.Clocks => LobbyNetworkManager.Instance.ClocksPuzzlePrefab,
            _ => throw new System.InvalidOperationException("Unsupported puzzle type encountered while spawning puzzles. Make sure all puzzle spawn points are set up properly.")
        };

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Puzzle with type {type} is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);

        var puzzle = gameObj.GetComponent<Puzzle>();
        puzzle.ServerSetType(type);

        return puzzle;
    }

    [Server]
    public ReflectableLightSource ServerSpawnReflectableLightSource(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.ReflectableLightSourcePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Reflectable Light Source is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<ReflectableLightSource>();
    }

    [Server]
    public ReflectableLightTarget ServerSpawnReflectableLightTarget(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.ReflectableLightTargetPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Reflectable Light Target is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<ReflectableLightTarget>();
    }

    [Server]
    public NetworkRoom ServerSpawnNetworkRoom(RoomType type, Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.NetworkRoomPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Network Room is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<NetworkRoom>();
    }

    [Server]
    public RotatingMirror ServerSpawnRotatingMirror(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.RotatingMirrorPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Rotating Mirror is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<RotatingMirror>();
    }

    [Server]
    public Statue ServerSpawnStatue(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.StatuePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Statue is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<Statue>();
    }

    [Server]
    public TimeCatcher ServerSpawnTimeCatcher(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.TimeCatcherPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Time Catcher is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<TimeCatcher>();
    }

    [Server]
    public TimeCatcherTrapDoor ServerSpawnTimeCatcherTrapDoor(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.TimeCatcherTrapDoorPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Time Catcher Trap Door is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
        NetworkServer.Spawn(gameObj);
        return gameObj.GetComponent<TimeCatcherTrapDoor>();
    }

    [Server]
    public GameObject ServerSpawnWindRose(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return null;

        var prefab = LobbyNetworkManager.Instance.WindRosePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"Spawnable prefab for Wind Rose is not set. The object will not be spawned.");
            return null;
        }

        var gameObj = Instantiate(prefab, position, rotation);
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
