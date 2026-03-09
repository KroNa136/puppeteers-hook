using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class WorldGenerator : NetworkBehaviour
{
    public static WorldGenerator Instance;

    public static UnityEvent<float> OnGenerationProgressUpdated = new();
    public static UnityEvent<float> OnReconstructionProgressUpdated = new();
    public static UnityEvent OnGenerationCompleted = new();
    public static UnityEvent<NetworkConnectionToClient, bool> OnReconstructionCompleted = new();

    [SerializeField] private GameObject[] _puzzles;
    [SerializeField] private GameObject[] _noteSpawnpoints;
    [SerializeField] private GameObject[] _amuletSpawnpoints;

    // TODO: Make WorldData serializable !!!
    public object WorldData { get; private set; }
    public string WorldHash { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
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

        // TODO: generate the world

        var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            OnGenerationProgressUpdated.Invoke((float) i / spawnPoints.Length);
            spawnPoints[i].ServerSpawn();
            yield return new WaitForSeconds(0.1f);
        }

        /*
        for (int i = 0; i < 5; i++)
        {
            OnGenerationProgressUpdated.Invoke(i / 5f);
            yield return new WaitForSeconds(0.1f);
        }
        */

        OnGenerationProgressUpdated.Invoke(1f);
        yield return new WaitForSeconds(1f);

        WorldData = (First: 1f, Second: 2f, Third: 3f);
        WorldHash = ComputeWorldHash(WorldData);

        OnGenerationCompleted.Invoke();
    }

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
}
