using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Door : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClientIsLockedChanged))]
    public bool IsLocked = false;

    [SyncVar(hook = nameof(OnClientIsOpenedChanged))]
    public bool IsOpened = false;

    [SerializeField] private DoorAudioController _audioController;

    private Interactable _interactable;
    private Transform _parent;

    [Space]

    [SerializeField] private float _openedAngle = -110f;
    [SerializeField] private AnimationCurve _openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _openingDuration = 1f;
    [SerializeField] private AnimationCurve _closingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _closingDuration = 1f;

    [Space]

    [SerializeField] private bool _ghostTransparencyRetainShadows = true;
    [SerializeField] private float _ghostTransparencyAlpha = 0.8f;

    private float _closedAngle;
    private Quaternion _openedRotation;
    private Quaternion _closedRotation;

    private Coroutine _animationCoroutine;
    private float _animationTimer = 0f;

    public bool IsAnimating => _animationCoroutine != null;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _ = TryGetComponent(out _interactable);

        _parent = transform.parent;

        if (!_parent.TryGetComponent(out NetworkTransformReliable _))
            Debug.LogWarning($"{gameObject.name}: door parent does not have a Network Transform (Reliable) component.");

        IsLocked = false;
        IsOpened = false;

        _closedAngle = _parent.localRotation.eulerAngles.y;

        _closedRotation = Quaternion.Euler(0f, _closedAngle, 0f);
        _openedRotation = Quaternion.Euler(0f, _closedAngle + _openedAngle, 0f);

        _parent.localRotation = IsOpened ? _openedRotation : _closedRotation;
        _animationTimer = IsOpened ? _openingDuration : _closingDuration;

        _ = StartCoroutine(ServerSetTransparencyForGhost());

        _ = _interactable.Bind(i => i.OnInteract.AddListener(ServerToggle));
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
        var renderers = GetComponentsInChildren<Renderer>().AsEnumerable();

        if (TryGetComponent(out Renderer renderer))
            renderers = renderers.Prepend(renderer);

        renderers
            .NonNullItems()
            .SelectMany(rend => rend.materials)
            .NonNullItems()
            .ForEach(mat =>
            {
                mat.SetFloat("_SurfaceType", 1);
                mat.SetFloat("_BlendMode", 0);
                mat.SetFloat("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_DstBlend2", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_AlphaSrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_AlphaDstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_AlphaCutoffEnable", 0);
                mat.SetFloat("_ZWrite", 0);
                mat.SetFloat("_TransparentZWrite", 1);
                mat.SetFloat("_ZTestDepthEqualForOpaque", 4);

                mat.SetShaderPassEnabled("ShadowCaster", _ghostTransparencyRetainShadows);
                mat.SetShaderPassEnabled("DepthOnly", false);
                mat.SetShaderPassEnabled("TransparentDepthPrepass", false);
                mat.SetShaderPassEnabled("TransparentDepthPostpass", false);
                mat.SetShaderPassEnabled("TransparentDepthBackface", false);
                mat.SetShaderPassEnabled("Forward", true);

                mat.SetOverrideTag("RenderType", "Transparent");

                mat.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;

                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                //mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                //mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                //mat.EnableKeyword("_ALPHABLEND_ON");

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.color = new
                    (
                        r: mat.color.r,
                        g: mat.color.g,
                        b: mat.color.b,
                        a: _ghostTransparencyAlpha
                    );
                }
            });
    }

    [Client]
    public override void OnStartClient()
    {
        if (!isClient)
            return;

        if (_interactable == null)
            _ = TryGetComponent(out _interactable);

        _ = _interactable.Bind(i => i.OnPredictInteraction.AddListener(ClientPredictToggle));
    }

    [Client]
    public void ClientPredictToggle()
    {
        if (!isClient)
            return;

        if (!IsOpened && IsLocked)
            ClientPlayLockedSound();
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
        //else
        //    RpcPlayLockedSound();
    }

    [Server]
    public void ServerOpen()
    {
        if (!isServer)
            return;

        if (IsLocked || IsOpened)
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

        if (IsLocked || IsOpened)
            return;

        IsLocked = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdUnlock(NetworkConnectionToClient conn = null)
    {
        if (!ServerHasGhostRole(conn))
            return;

        if (!IsLocked)
            return;

        IsLocked = false;
    }

    [Server]
    public void ServerLock()
    {
        if (!isServer)
            return;

        if (IsLocked || IsOpened)
            return;

        IsLocked = true;
    }

    [Server]
    public void ServerUnlock()
    {
        if (!isServer)
            return;

        if (!IsLocked)
            return;

        IsLocked = false;
    }

    [Server]
    public bool ServerHasGhostRole(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return false;

        var playerData = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn);
        return playerData != null && playerData.Role is PlayerRole.Ghost;
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

        var (curve, duration, startRotation, endRotation, onFinished) = opening
            ? (_openingCurve, _openingDuration, _closedRotation, _openedRotation, null)
            : (_closingCurve, _closingDuration, _openedRotation, _closedRotation, (UnityAction) RpcEnableHighSanityLossSounds);

        _animationTimer = opening
            ? (1f - (_animationTimer / _closingDuration)) / _openingDuration
            : (1f - (_animationTimer / _openingDuration)) / _closingDuration;

        _animationCoroutine = StartCoroutine(ServerAnimate(curve, duration, startRotation, endRotation, onFinished));

        DisableInteraction();
        RpcDisableInteraction();
    }

    [Server]
    public IEnumerator ServerAnimate(AnimationCurve curve, float duration, Quaternion startRotation, Quaternion endRotation, UnityAction onFinished = null)
    {
        if (!isServer)
            yield break;

        while (_animationTimer < duration)
        {
            _animationTimer += Time.deltaTime;
            float interpolator = Mathf.Clamp01(_animationTimer / duration);

            _parent.localRotation = Quaternion.Slerp(startRotation, endRotation, curve.Evaluate(interpolator));
            yield return null;
        }

        _parent.localRotation = endRotation;
        onFinished?.Invoke();
        _animationCoroutine = null;

        EnableInteraction();
        RpcEnableInteraction();
    }

    [ClientRpc]
    public void RpcEnableHighSanityLossSounds()
    {
        _ = _audioController.Bind(c => c.enableHighSanityLossSounds = true);
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

    /*
    [ClientRpc]
    public void RpcPlayLockedSound()
    {
        _ = _audioController.Bind(c => c.PlayLockedSound());
    }
    */

    [Client]
    public void ClientPlayLockedSound()
    {
        _ = _audioController.Bind(c => c.PlayLockedSound());
    }

    [Client]
    public void OnClientIsOpenedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;

        _ = _audioController.Bind(newValue ? c => c.PlayOpeningSound() : c => c.PlayClosingSound());
    }

    [Client]
    public void OnClientIsLockedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;

        _ = _audioController.Bind(newValue ? c => c.PlayLockedSound() : c => c.PlayUnlockedSound());
    }

    /*
    [SyncVar(hook = nameof(OnClientIsLockedChanged))]
    public bool IsLocked = false;

    [SyncVar]
    public bool IsOpened = false;
    private bool _predictedIsOpened = false;

    [SerializeField] private Transform _parent;
    [SerializeField] private DoorAudioController _audioController;

    [Space]

    [SerializeField] private float _openedAngle = -110f;
    [SerializeField] private AnimationCurve _openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _openingDuration = 1f;
    [SerializeField] private AnimationCurve _closingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _closingDuration = 1f;

    [Space]

    [SerializeField] private bool _ghostTransparencyRetainShadows = true;
    [SerializeField] private float _ghostTransparencyAlpha = 0.8f;

    private float _closedAngle;
    private Quaternion _openedRotation;
    private Quaternion _closedRotation;

    private Coroutine _animationCoroutine;
    private float _animationTimer = 0f;

    private void Start()
    {
        _closedAngle = _parent.localRotation.eulerAngles.y;

        _openedRotation = Quaternion.Euler(0f, _openedAngle, 0f);
        _closedRotation = Quaternion.Euler(0f, _closedAngle, 0f);
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        if (TryGetComponent(out Interactable interactable))
            interactable.OnInteract.AddListener(ServerToggle);

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
        var renderers = GetComponentsInChildren<Renderer>().AsEnumerable();

        if (TryGetComponent(out Renderer renderer))
            renderers = renderers.Prepend(renderer);

        renderers
            .NonNullItems()
            .SelectMany(renderer => renderer.materials)
            .NonNullItems()
            .ForEach(mat =>
            {
                mat.SetFloat("_SurfaceType", 1);
                mat.SetFloat("_BlendMode", 0);
                mat.SetFloat("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_DstBlend2", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_AlphaSrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_AlphaDstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_AlphaCutoffEnable", 0);
                //mat.SetFloat("_ZWrite", 0);
                mat.SetFloat("_TransparentZWrite", 1);
                mat.SetFloat("_ZTestDepthEqualForOpaque", 4);

                mat.SetShaderPassEnabled("ShadowCaster", _ghostTransparencyRetainShadows);
                mat.SetShaderPassEnabled("DepthOnly", false);
                mat.SetShaderPassEnabled("TransparentDepthPrepass", false);
                mat.SetShaderPassEnabled("TransparentDepthPostpass", false);
                mat.SetShaderPassEnabled("TransparentDepthBackface", false);
                mat.SetShaderPassEnabled("Forward", true);

                mat.SetOverrideTag("RenderType", "Transparent");

                mat.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;

                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                //mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                //mat.EnableKeyword("_ALPHABLEND_ON");

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.color = new
                    (
                        r: mat.color.r,
                        g: mat.color.g,
                        b: mat.color.b,
                        a: _ghostTransparencyAlpha
                    );
                }
            });
    }

    [Client]
    public override void OnStartClient()
    {
        if (!isClient)
            return;

        _parent.localRotation = IsOpened ? _openedRotation : _closedRotation;
        _animationTimer = IsOpened ? _openingDuration : _closingDuration;

        if (TryGetComponent(out Interactable interactable))
            interactable.OnPredictInteraction.AddListener(ClientPredictToggle);
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

    [Client]
    public void ClientPredictToggle()
    {
        if (!isClient)
            return;

        Debug.Log($"Client predict toggle: predicted IsOpened = {_predictedIsOpened} | IsOpened = {IsOpened} | IsLocked = {IsLocked}");

        if (_predictedIsOpened)
            ClientClose();
        else if (!IsLocked)
            ClientOpen();
        else
            ClientPlayLockedSound();
    }

    [ClientRpc]
    public void RpcOpen()
    {
        ClientOpen();
    }

    [Client]
    public void ClientOpen()
    {
        if (!isClient)
            return;

        if (_predictedIsOpened || IsLocked)
            return;

        _predictedIsOpened = true;

        ClientStopAnimation();
        ClientStartAnimation(opening: true);

        _ = _audioController
            .Bind(c => c.enableHighSanityLossSounds = false)
            .Bind(c => c.PlayOpeningSound());
    }

    [ClientRpc]
    public void RpcClose()
    {
        ClientClose();
    }

    [Client]
    public void ClientClose()
    {
        if (!isClient)
            return;

        if (!_predictedIsOpened || IsLocked)
            return;

        _predictedIsOpened = false;

        ClientStopAnimation();
        ClientStartAnimation(opening: false);

        _ = _audioController
            .Bind(c => c.enableHighSanityLossSounds = false)
            .Bind(c => c.PlayClosingSound());
    }

    [ClientRpc]
    public void RpcPlayLockedSound()
    {
        ClientPlayLockedSound();
    }

    [Client]
    public void ClientPlayLockedSound()
    {
        if (!isClient)
            return;

        _ = _audioController.Bind(c => c.PlayLockedSound());
    }

    [ClientRpc]
    public void RpcPlayUnlockedSound()
    {
        ClientPlayUnlockedSound();
    }

    [Client]
    public void ClientPlayUnlockedSound()
    {
        if (!isClient)
            return;

        _ = _audioController.Bind(c => c.PlayUnlockedSound());
    }

    [Client]
    public void OnClientIsLockedChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;

        ClientStopAnimation();
        _parent.localRotation = _closedRotation;
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

        var (curve, duration, startRotation, endRotation, onFinished) = opening
            ? (_openingCurve, _openingDuration, _closedRotation, _openedRotation, null)
            : (_closingCurve, _closingDuration, _openedRotation, _closedRotation, (UnityAction) ClientEnableHighSanityLossSounds);

        _animationTimer = opening
            ? (1f - (_animationTimer / _closingDuration)) / _openingDuration
            : (1f - (_animationTimer / _openingDuration)) / _closingDuration;

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
            
            _parent.localRotation = Quaternion.Slerp(startRotation, endRotation, curve.Evaluate(interpolator));
            yield return null;
        }

        _parent.localRotation = endRotation;
        onFinished?.Invoke();
    }

    [Client]
    public void ClientEnableHighSanityLossSounds()
    {
        if (!isClient)
            return;

        _ = _audioController.Bind(c => c.enableHighSanityLossSounds = true);
    }
    */
}
