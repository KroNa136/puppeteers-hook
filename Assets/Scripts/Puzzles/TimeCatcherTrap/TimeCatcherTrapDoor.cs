using System.Collections;
using Mirror;
using UnityEngine;

public class TimeCatcherTrapDoor : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClientIsOpenedChanged))]
    public bool IsOpened = false;

    [SerializeField] private Transform _doorObject;
    [SerializeField] private TimeCatcherTrapDoorAudioController _audioController;

    private Interactable _interactable;

    [Space]

    [SerializeField] private float _openedAngle = 90f;
    [SerializeField] private AnimationCurve _openingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _openingDuration = 1f;
    [SerializeField] private AnimationCurve _closingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _closingDuration = 1f;

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

        IsOpened = false;

        _closedAngle = _doorObject.localRotation.eulerAngles.z;

        _closedRotation = Quaternion.Euler(0f, 0f, _closedAngle);
        _openedRotation = Quaternion.Euler(0f, 0f, _closedAngle + _openedAngle);

        _doorObject.localRotation = IsOpened ? _openedRotation : _closedRotation;
        _animationTimer = IsOpened ? _openingDuration : _closingDuration;

        _ = _interactable.Bind(i => i.OnInteract.AddListener(ServerToggle));
    }

    [Server]
    public void ServerNegateOpenedAngle()
    {
        if (!isServer)
            return;

        _openedAngle = -_openedAngle;
        _openedRotation = Quaternion.Euler(0f, _closedAngle + _openedAngle, 0f);

        _doorObject.localRotation = IsOpened ? _openedRotation : _closedRotation;
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

        var (curve, duration, startRotation, endRotation) = opening
            ? (_openingCurve, _openingDuration, _closedRotation, _openedRotation)
            : (_closingCurve, _closingDuration, _openedRotation, _closedRotation);

        _animationTimer = opening
            ? (1f - (_animationTimer / _closingDuration)) / _openingDuration
            : (1f - (_animationTimer / _openingDuration)) / _closingDuration;

        _animationCoroutine = StartCoroutine(ServerAnimate(curve, duration, startRotation, endRotation));

        ServerDisableInteraction();
    }

    [Server]
    public IEnumerator ServerAnimate(AnimationCurve curve, float duration, Quaternion startRotation, Quaternion endRotation)
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
        _animationCoroutine = null;

        EnableInteraction();
        RpcEnableInteraction();
    }

    [Server]
    public void ServerDisableInteraction()
    {
        if (!isServer)
            return;

        DisableInteraction();
        RpcDisableInteraction();
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
