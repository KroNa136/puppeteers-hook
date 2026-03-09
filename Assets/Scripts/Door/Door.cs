using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Door : NetworkBehaviour
{
    [SyncVar]
    [HideInInspector] public bool IsLocked = false;

    [SyncVar]
    [HideInInspector] public bool IsOpened = false;

    [SerializeField] private DoorAudioController _audioController;

    [Space]

    [SerializeField] private float _openedAngle = -110f;
    [SerializeField] private AnimationCurve _openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _openingDuration = 1f;
    [SerializeField] private AnimationCurve _closingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _closingDuration = 1f;

    [Space]

    [SerializeField] private bool _ghostTransparencyRetainShadows = true;
    [SerializeField] private float _ghostTransparencyAlpha = 0.5f;

    private float _closedAngle;
    private Quaternion _openedRotation;
    private Quaternion _closedRotation;

    private Coroutine _animationCoroutine;
    private float _animationTimer = 0f;

    private void Start()
    {
        _closedAngle = transform.rotation.eulerAngles.y;

        _openedRotation = Quaternion.Euler(0f, _openedAngle, 0f);
        _closedRotation = Quaternion.Euler(0f, _closedAngle, 0f);
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _ = StartCoroutine(ServerSetTransparencyForGhost());
    }

    [Server]
    public IEnumerator ServerSetTransparencyForGhost()
    {
        if (!isServer)
            yield break;

        NetworkConnectionToClient ghostConnection = null;

        while (ghostConnection == null)
        {
            ghostConnection = LobbyNetworkManager.Instance.GetConnectionForRole(PlayerRole.Ghost);
            yield return null;
        }

        TargetRpcSetTransparency(ghostConnection);
    }

    [TargetRpc]
    public void TargetRpcSetTransparency(NetworkConnectionToClient conn)
    {
        var renderers = GetComponentsInChildren<Renderer>()
            .Prepend(GetComponent<Renderer>());

        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            foreach (var material in renderer.materials)
            {
                if (material == null)
                    continue;

                material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                //material.SetInt("_ZWrite", 0);
                material.SetInt("_Surface", 1);

                material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;

                material.SetShaderPassEnabled("DepthOnly", false);
                material.SetShaderPassEnabled("SHADOWCASTER", _ghostTransparencyRetainShadows);

                material.SetOverrideTag("RenderType", "Transparent");

                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");

                if (material.HasProperty("_BaseColor"))
                {
                    material.color = new
                    (
                        r: material.color.r,
                        g: material.color.g,
                        b: material.color.b,
                        a: _ghostTransparencyAlpha
                    );
                }
            }
        }
    }

    [Client]
    public override void OnStartClient()
    {
        if (!isClient)
            return;

        transform.localRotation = IsOpened ? _openedRotation : _closedRotation;
        _animationTimer = IsOpened ? _openingDuration : _closingDuration;
    }

    [Server]
    public void ServerToggle(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        if (IsOpened)
            ServerClose();
        else if (!IsLocked)
            ServerOpen();
        else
            RpcPlayLockedSound();
    }

    [Server]
    public void ServerOpen()
    {
        if (!isServer)
            return;

        if (IsLocked || IsOpened)
            return;

        IsOpened = true;

        RpcOpen();
    }

    [Server]
    public void ServerClose()
    {
        if (!isServer)
            return;

        if (!IsOpened)
            return;

        IsOpened = false;

        RpcClose();
    }

    [Command(requiresAuthority = false)]
    public void CmdClose(NetworkConnectionToClient conn = null)
    {
        if (!ServerHasGhostRole(conn))
            return;

        ServerClose();
    }

    [Command(requiresAuthority = false)]
    public void CmdLock(NetworkConnectionToClient conn = null)
    {
        if (!ServerHasGhostRole(conn))
            return;

        IsLocked = true;
        RpcPlayLockedSound();
    }

    [Command(requiresAuthority = false)]
    public void CmdUnlock(NetworkConnectionToClient conn = null)
    {
        if (!ServerHasGhostRole(conn))
            return;

        IsLocked = false;
        RpcPlayUnlockedSound();
    }

    [Server]
    public bool ServerHasGhostRole(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return false;

        var playerData = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn);
        return playerData != null && playerData.Role is PlayerRole.Ghost;
    }

    [ClientRpc]
    public void RpcOpen()
    {
        ClientStopAnimation();
        ClientStartAnimation(opening: true);

        _audioController.enableHighSanityLossSounds = false;
        _audioController.PlayOpeningSound();
    }

    [ClientRpc]
    public void RpcClose()
    {
        ClientStopAnimation();
        ClientStartAnimation(opening: false);

        _audioController.enableHighSanityLossSounds = false;
        _audioController.PlayClosingSound();
    }

    [ClientRpc]
    public void RpcPlayLockedSound()
    {
        _audioController.PlayLockedSound();
    }

    [ClientRpc]
    public void RpcPlayUnlockedSound()
    {
        _audioController.PlayUnlockedSound();
    }

    [Client]
    public void ClientStopAnimation()
    {
        if (!isClient)
            return;

        if (_animationCoroutine == null)
            return;

        StopCoroutine(_animationCoroutine);
        _animationCoroutine = null;
    }

    [Client]
    public void ClientStartAnimation(bool opening)
    {
        if (!isClient)
            return;

        var (curve, duration, startRotation, endRotation, onFinished) = opening switch
        {
            true => (_openingCurve, _openingDuration, _closedRotation, _openedRotation, null),
            false => (_closingCurve, _closingDuration, _openedRotation, _closedRotation, (UnityAction) ClientEnableHighSanityLossSounds)
        };

        _animationTimer = opening ?
            (1f - (_animationTimer / _closingDuration)) / _openingDuration :
            (1f - (_animationTimer / _openingDuration)) / _closingDuration;

        _animationCoroutine = StartCoroutine(ClientAnimate(curve, duration, startRotation, endRotation, onFinished));
    }

    [Client]
    public IEnumerator ClientAnimate(AnimationCurve curve, float duration, Quaternion startRotation, Quaternion endRotation, UnityAction onFinished = null)
    {
        if (!isClient)
            yield break;

        while (_animationTimer < duration)
        {
            _animationTimer += Time.deltaTime;
            float interpolator = Mathf.Clamp01(_animationTimer / duration);
            
            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, curve.Evaluate(interpolator));
            yield return null;
        }

        transform.localRotation = endRotation;
        onFinished?.Invoke();
    }

    [Client]
    public void ClientEnableHighSanityLossSounds()
    {
        if (!isClient)
            return;

        _audioController.enableHighSanityLossSounds = true;
    }
}
