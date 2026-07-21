using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Audio;

public class FearManager : NetworkBehaviour
{
    [SerializeField] private InvestigatorAudioController _audioController;
    [SerializeField] private AudioMixer _audioMixer;

    [Space]

    [SerializeField] private float _maxFear = 100f;
    [SerializeField] private float _maxDistanceToFearCause = 5f;

    [Space]

    [SerializeField] private float _cameraShakeSpeed = 0.5f;
    [SerializeField] private float _cameraShakeDuration = 1f;
    [SerializeField] private Vector3 _maxCameraShakeOffset = new(0.05f, 0.05f, 0.05f);

    private Coroutine _cameraShakeCoroutine;

    [SyncVar(hook = nameof(OnClientCurrentFearChanged))]
    public float CurrentFear;

    private readonly List<Transform> _fearCauses = new();

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        TickSystem.Instance.OnTick.AddListener(ServerTick);
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        if (_fearCauses.None())
            return;

        _ = _fearCauses.RemoveAll(cause => Vector3.Distance(cause.position, transform.position) > _maxDistanceToFearCause);

        if (_fearCauses.None())
        {
            RpcStopBeingAfraid();
            return;
        }

        ServerSetCurrentFear();
    }

    [Server]
    public void ServerStartBeingAfraidOf(Transform cause)
    {
        if (!isServer)
            return;

        if (cause == null)
            return;

        if (TryGetComponent(out Inventory inventory) && inventory.HasAmulet)
        {
            _ = inventory.ServerSpendAmulet();
            return;
        }

        if (!_fearCauses.Contains(cause))
            _fearCauses.Add(cause);

        RpcStartBeingAfraid();
        ServerSetCurrentFear();
    }

    [Server]
    public bool ServerIsAfraidOf(Transform cause) => isServer && _fearCauses.Contains(cause);

    [Server]
    public void ServerStopBeingAfraidOf(Transform cause)
    {
        if (!isServer)
            return;

        _ = _fearCauses.Remove(cause);

        if (_fearCauses.None())
            RpcStopBeingAfraid();
    }

    [Server]
    public void ServerSetCurrentFear()
    {
        if (!isServer)
            return;

        float distanceToClosestFearCause = _fearCauses
            .Select(cause => Vector3.Distance(cause.position, transform.position))
            .Min();

        float clampedDistanceToClosestFearCause = Mathf.Clamp
        (
            value: distanceToClosestFearCause,
            min: 0f,
            max: _maxDistanceToFearCause
        );

        CurrentFear = _maxFear * (1f - (clampedDistanceToClosestFearCause / _maxDistanceToFearCause));
    }

    [ClientRpc]
    public void RpcStartBeingAfraid()
    {
        if (_cameraShakeCoroutine != null)
        {
            StopCoroutine(_cameraShakeCoroutine);
            _cameraShakeCoroutine = null;
        }

        if (!isLocalPlayer)
            return;

        _cameraShakeCoroutine = StartCoroutine(CameraShake());

        _ = _audioController.Bind(c => c.PlayFearSound());
    }

    [ClientRpc]
    public void RpcStopBeingAfraid()
    {
        if (!isLocalPlayer)
            return;

        _ = _audioMixer
            .Bind((mixer, param, value) => mixer.SetFloat(param, value), "world_echo_wetmix", 0f)
            .Bind((mixer, param, value) => mixer.SetFloat(param, value), "world_lowpass_cutoff_freq", 22000f);

        _ = _audioController.Bind(c => c.StopFearSound());
    }

    [Client]
    public IEnumerator CameraShake()
    {
        if (!isLocalPlayer)
            yield break;

        Transform camera = Camera.main.transform;
        float t = 0f;
        Vector3 initialLocalPosition = camera.localPosition;
        Vector3 targetLocalPosition = initialLocalPosition;

        while (t < _cameraShakeDuration)
        {
            if (Vector3.Distance(camera.localPosition, targetLocalPosition) < 0.005f)
            {
                Vector3 offset = new
                (
                    x: Random.Range(-_maxCameraShakeOffset.x, _maxCameraShakeOffset.x),
                    y: Random.Range(-_maxCameraShakeOffset.y, _maxCameraShakeOffset.y),
                    z: Random.Range(-_maxCameraShakeOffset.z, _maxCameraShakeOffset.z)
                );

                float intensity = 1f - (t / _cameraShakeDuration);

                targetLocalPosition = initialLocalPosition + (offset * intensity);
            }

            camera.localPosition = Vector3.MoveTowards(camera.localPosition, targetLocalPosition, _cameraShakeSpeed * Time.deltaTime);

            t += Time.deltaTime;
            yield return null;
        }

        while (Vector3.Distance(camera.localPosition, initialLocalPosition) >= 0.005f)
        {
            camera.localPosition = Vector3.MoveTowards(camera.localPosition, initialLocalPosition, _cameraShakeSpeed * Time.deltaTime);
            yield return null;
        }

        camera.localPosition = initialLocalPosition;

        _cameraShakeCoroutine = null;
    }

    [Client]
    public void OnClientCurrentFearChanged(float oldValue, float newValue)
    {
        if (!isLocalPlayer)
            return;

        float fearFraction = newValue / _maxFear;

        _ = _audioMixer
            .Bind((mixer, param, value) => mixer.SetFloat(param, value), "world_echo_wetmix", 0.7f * fearFraction)
            .Bind((mixer, param, value) => mixer.SetFloat(param, value), "world_lowpass_cutoff_freq", 1500f + (22000f - 1500f) * fearFraction);

        _ = _audioController.Bind((controller, volume) => controller.SetFearVolume(volume), fearFraction);
    }
}
