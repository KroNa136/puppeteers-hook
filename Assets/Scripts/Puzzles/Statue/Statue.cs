using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class Statue : NetworkBehaviour
{
    public UnityEvent OnServerFinishedRotation = new();

    // [SerializeField] private StatueAudioController _audioController;

    [SyncVar(hook = nameof(OnClientIsRotatingChanged))]
    public bool IsRotating;

    [SerializeField] private bool _counterClockwiseRotation;
    [SerializeField] private float _rotationSpeed = 45f;
    [SerializeField] private float _rotationAngleDelta = 45f;

    public float RotationAngleDelta => _rotationAngleDelta;

    private float _targetRotationAngle;

    private Coroutine _animationCoroutine;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        if (TryGetComponent(out Interactable interactable))
            interactable.OnInteract.AddListener(ServerRotate);

        _targetRotationAngle = transform.eulerAngles.y;
        IsRotating = false;
    }

    [Server]
    public void ServerRotate(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        _targetRotationAngle += _rotationAngleDelta;

        while (_targetRotationAngle >= 360f)
            _targetRotationAngle -= 360f;

        _animationCoroutine ??= StartCoroutine(ServerAnimate());
    }

    [Server]
    public IEnumerator ServerAnimate()
    {
        if (!isServer)
            yield break;

        IsRotating = true;

        while (Mathf.DeltaAngle(transform.eulerAngles.y, _targetRotationAngle) > 1f)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, _targetRotationAngle, 0f);

            transform.rotation = Quaternion.RotateTowards
            (
                from: transform.rotation,
                to: targetRotation,
                maxDegreesDelta: _rotationSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.eulerAngles = new Vector3(0f, _targetRotationAngle, 0f);

        IsRotating = false;
        _animationCoroutine = null;

        OnServerFinishedRotation.Invoke();
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
        if (TryGetComponent(out Interactable interactable))
            interactable.enabled = false;
    }

    [Client]
    public void OnClientIsRotatingChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;

        // _ = _audioController.Bind(newValue ? c => c.PlayRotationSound() : c => c.StopRotationSound());
    }
}
