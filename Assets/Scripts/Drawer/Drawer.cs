using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class Drawer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClientIsOpenedChanged))]
    public bool IsOpened = false;

    [SerializeField] private Transform _drawerObject;
    [SerializeField] private DrawerAudioController _audioController;

    private Interactable _interactable;

    [Space]

    [SerializeField] private float _openedZOffset = -0.5f;
    [SerializeField] private AnimationCurve _openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _openingDuration = 1f;
    [SerializeField] private AnimationCurve _closingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _closingDuration = 1f;

    private Vector3 _openedPosition;
    private Vector3 _closedPosition;

    private Coroutine _animationCoroutine;
    private float _animationTimer = 0f;

    public bool IsAnimating => _animationCoroutine != null;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _ = TryGetComponent(out _interactable);

        IsOpened = false;

        _closedPosition = _drawerObject.localPosition;
        _openedPosition = _closedPosition + transform.forward * _openedZOffset;

        _drawerObject.localPosition = IsOpened ? _openedPosition : _closedPosition;
        _animationTimer = IsOpened ? _openingDuration : _closingDuration;

        _ = _interactable.Bind(i => i.OnInteract.AddListener(ServerToggle));
    }

    [Server]
    public IEnumerator ServerInitialize()
    {
        if (!isServer)
            yield break;

        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        var amuletSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.DrawerAmulet);
        var noteSpawnPoints = spawnPoints.Where(sp => sp.Type is SpawnPointType.DrawerNote);

        foreach (var spawnPoint in amuletSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            var amulet = WorldGenerator.Instance.ServerSpawnAmulet(spawnPoint.transform.position, spawnPoint.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            RpcSetParentForAmulet(amulet.netId);

            yield return new WaitForSeconds(0.3f);
        }

        foreach (var spawnPoint in noteSpawnPoints)
        {
            if (spawnPoint.SpawnChance == 0f)
                continue;

            bool spawn = Random.value <= spawnPoint.SpawnChance;

            if (!spawn)
                continue;

            var note = WorldGenerator.Instance.ServerSpawnNote(spawnPoint.transform.position, spawnPoint.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            note.ServerSetLoreText();
            yield return new WaitForSeconds(0.3f);
            RpcSetParentForNote(note.netId);

            yield return new WaitForSeconds(0.3f);
        }
    }

    [ClientRpc]
    public void RpcSetParentForAmulet(uint netId)
    {
        var amulets = FindObjectsByType<Amulet>(FindObjectsSortMode.None);
        var amuletToSetParentFor = amulets.FirstOrDefault(amulet => amulet.netId == netId);

        if (amuletToSetParentFor == null)
            return;

        amuletToSetParentFor.transform.SetParent(_drawerObject, worldPositionStays: true);
    }

    [ClientRpc]
    public void RpcSetParentForNote(uint netId)
    {
        var notes = FindObjectsByType<Note>(FindObjectsSortMode.None);
        var noteToSetParentFor = notes.FirstOrDefault(note => note.netId == netId);

        if (noteToSetParentFor == null)
            return;

        noteToSetParentFor.transform.SetParent(_drawerObject, worldPositionStays: true);
    }

    [Server]
    public void ServerToggle(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        if (IsOpened)
            ServerClose();
        else
            ServerOpen();
    }

    [Server]
    public void ServerOpen()
    {
        if (!isServer)
            return;

        if (IsOpened)
            return;

        IsOpened = true;

        ServerStopAnimation();
        ServerStartAnimation(opening: true);
    }

    [Server]
    public void ServerClose()
    {
        if (!isServer)
            return;

        if (!IsOpened)
            return;

        IsOpened = false;

        ServerStopAnimation();
        ServerStartAnimation(opening: false);
    }

    [Server]
    public void ServerStopAnimation()
    {
        if (!isServer)
            return;

        if (_animationCoroutine == null)
            return;

        StopCoroutine(_animationCoroutine);
        _animationCoroutine = null;

        EnableInteraction();
        RpcEnableInteraction();
    }

    [Server]
    public void ServerStartAnimation(bool opening)
    {
        if (!isServer)
            return;

        var (curve, duration, startPosition, endPosition) = opening
            ? (_openingCurve, _openingDuration, _closedPosition, _openedPosition)
            : (_closingCurve, _closingDuration, _openedPosition, _closedPosition);

        _animationTimer = opening
            ? (1f - (_animationTimer / _closingDuration)) / _openingDuration
            : (1f - (_animationTimer / _openingDuration)) / _closingDuration;

        _animationCoroutine = StartCoroutine(ServerAnimate(curve, duration, startPosition, endPosition));

        DisableInteraction();
        RpcDisableInteraction();
    }

    [Server]
    public IEnumerator ServerAnimate(AnimationCurve curve, float duration, Vector3 startPosition, Vector3 endPosition)
    {
        if (!isServer)
            yield break;

        while (_animationTimer < duration)
        {
            _animationTimer += Time.deltaTime;
            float interpolator = Mathf.Clamp01(_animationTimer / duration);

            _drawerObject.localPosition = Vector3.Lerp(startPosition, endPosition, curve.Evaluate(interpolator));
            yield return null;
        }

        _drawerObject.localPosition = endPosition;
        _animationCoroutine = null;

        EnableInteraction();
        RpcEnableInteraction();
    }

    [ClientRpc]
    public void RpcDisableInteraction()
    {
        if (isServer)
            return;

        DisableInteraction();
    }

    private void DisableInteraction()
    {
        _ = _interactable.Bind(i => i.enabled = false);
    }

    [ClientRpc]
    public void RpcEnableInteraction()
    {
        if (isServer)
            return;

        EnableInteraction();
    }

    private void EnableInteraction()
    {
        _ = _interactable.Bind(i => i.enabled = true);
    }

    [Client]
    public void OnClientIsOpenedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;
        
        _ = _audioController.Bind(newValue ? c => c.PlayOpeningSound() : c => c.PlayClosingSound());
    }
}
