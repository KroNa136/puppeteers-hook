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

    [SerializeField] private Transform _doorObject;
    [SerializeField] private DoorAudioController _audioController;

    private Interactable _interactable;

    [Space]

    [SerializeField] private float _openedAngle = -110f;
    [SerializeField] private AnimationCurve _openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _openingDuration = 1f;
    [SerializeField] private AnimationCurve _closingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _closingDuration = 1f;

    [Space]

    [SerializeField] private bool _ghostTransparencyRetainShadows = true;
    [SerializeField] private float _ghostTransparencyAlpha = 0.05f;

    [Space]

    [SerializeField] private string _permanentlyLockedDoorLayer;

    private float _closedAngle;
    private Quaternion _openedRotation;
    private Quaternion _closedRotation;

    private Coroutine _animationCoroutine;
    private float _animationTimer = 0f;

    public bool IsAnimating => _animationCoroutine != null;

    private bool _predictedLockSound;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _ = TryGetComponent(out _interactable);

        IsLocked = false;
        IsOpened = false;

        _closedAngle = _doorObject.localRotation.eulerAngles.y;

        _closedRotation = Quaternion.Euler(0f, _closedAngle, 0f);
        _openedRotation = Quaternion.Euler(0f, _closedAngle + _openedAngle, 0f);

        _doorObject.localRotation = IsOpened ? _openedRotation : _closedRotation;
        _animationTimer = IsOpened ? _openingDuration : _closingDuration;

        _ = StartCoroutine(ServerSetAlphaForGhost(_ghostTransparencyAlpha));

        _ = _interactable.Bind(i => i.OnInteract.AddListener(ServerToggle));
    }

    [Server]
    public IEnumerator ServerSetAlphaForGhost(float alpha)
    {
        if (!isServer)
            yield break;

        NetworkConnectionToClient ghostConnection = null;

        while (ghostConnection == null)
        {
            ghostConnection = LobbyNetworkManager.Instance.GetConnectionForRole(PlayerRole.Ghost);
            yield return null;
        }

        TargetRpcSetAlpha(ghostConnection, alpha);
    }

    [TargetRpc]
    public void TargetRpcSetAlpha(NetworkConnectionToClient conn, float alpha)
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

                if (!mat.HasProperty("_BaseColor"))
                    return;

                mat.color = new
                (
                    r: mat.color.r,
                    g: mat.color.g,
                    b: mat.color.b,
                    a: alpha
                );
            });
    }

    [Client]
    public override void OnStartClient()
    {
        if (!isClient)
            return;

        _predictedLockSound = false;

        if (_interactable == null)
            _ = TryGetComponent(out _interactable);

        _ = _interactable.Bind(i => i.OnPredictInteraction.AddListener(ClientPredictToggle));
    }

    [Client]
    public void ClientPredictToggle()
    {
        if (!isClient || isServer)
            return;

        if (!IsOpened && IsLocked)
        {
            _predictedLockSound = false;
            ClientPlayLockSound();
            _predictedLockSound = true;
        }
    }

    [Server]
    public void ServerToggle(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        if (IsOpened)
        {
            ServerClose();
        }
        else if (!IsLocked)
        {
            ServerOpen();
        }
        else
        {
            RpcPlayLockSound();
            ClientPlayLockSound();
        }
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

    [Server]
    public void ServerLock()
    {
        if (!isServer)
            return;

        if (IsLocked || IsOpened)
            return;

        IsLocked = true;

        _ = StartCoroutine(ServerSetAlphaForGhost(1f));
    }

    [Server]
    public void ServerUnlock()
    {
        if (!isServer)
            return;

        if (!IsLocked)
            return;

        IsLocked = false;

        _ = StartCoroutine(ServerSetAlphaForGhost(_ghostTransparencyAlpha));
    }

    [Server]
    public void ServerLockPermanently()
    {
        if (!isServer)
            return;

        ServerLock();
        SetPermanentlyLockedLayer();
        RpcSetPermanentlyLockedLayer();
    }

    [ClientRpc]
    public void RpcSetPermanentlyLockedLayer()
    {
        if (isServer)
            return;

        SetPermanentlyLockedLayer();
    }

    private void SetPermanentlyLockedLayer()
    {
        int layer = LayerMask.NameToLayer(_permanentlyLockedDoorLayer);

        gameObject.layer = layer;
        foreach (Transform child in transform)
            child.gameObject.layer = layer;
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
            : (_closingCurve, _closingDuration, _openedRotation, _closedRotation, (UnityAction) RpcEnableSanityLossSounds);

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

            _doorObject.localRotation = Quaternion.Slerp(startRotation, endRotation, curve.Evaluate(interpolator));
            yield return null;
        }

        _doorObject.localRotation = endRotation;
        onFinished?.Invoke();
        _animationCoroutine = null;

        EnableInteraction();
        RpcEnableInteraction();
    }

    [ClientRpc]
    public void RpcEnableSanityLossSounds()
    {
        _ = _audioController.Bind(c => c.CanPlaySanityLossSounds = true);
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

    [ClientRpc]
    public void RpcPlayLockSound()
    {
        if (isServer)
            return;

        ClientPlayLockSound();
    }

    [Client]
    public void ClientPlayLockSound()
    {
        if (!isClient)
            return;

        if (_predictedLockSound)
        {
            _predictedLockSound = false;
            return;
        }    

        _ = _audioController.Bind(c => c.PlayLockSound());
    }

    [Client]
    public void OnClientIsOpenedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;

        _predictedLockSound = false;

        _ = _audioController.Bind(newValue ? c => c.PlayOpeningSound() : c => c.PlayClosingSound());
    }

    [Client]
    public void OnClientIsLockedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;

        _predictedLockSound = false;

        _ = _audioController.Bind(newValue ? c => c.PlayLockSound() : c => c.PlayUnlockSound());
    }
}
