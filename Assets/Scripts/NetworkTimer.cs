using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NetworkTimer : NetworkBehaviour
{
    public UnityEvent OnStarted = new();
    public UnityEvent<int> OnUpdated = new();
    public UnityEvent OnStopped = new();
    public UnityEvent OnTimeRanOut = new();

    [SyncVar(hook = nameof(OnClientCurrentSecondsChanged))]
    public int CurrentSeconds;

    [SyncVar]
    public bool IsRunning;

    private readonly WaitForSeconds _waitForOneSecond = new(1f);

    private Coroutine _timerCoroutine;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        IsRunning = false;
    }

    [Server]
    public void ServerStartTimer(int seconds)
    {
        if (!isServer)
            return;

        if (IsRunning)
        {
            Debug.LogError("Attempted to start a Network Timer that is already running.");
            return;
        }

        if (seconds <= 0)
        {
            Debug.LogError("Attempted to start a Network Timer with 0 or less seconds.");
            return;
        }

        IsRunning = true;
        _timerCoroutine = StartCoroutine(ServerTimerCoroutine(seconds));

        OnStarted.Invoke();
    }

    [Server]
    public void ServerStopTimer()
    {
        if (!isServer)
            return;

        if (!IsRunning)
            return;

        IsRunning = false;

        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        OnStopped.Invoke();
    }

    [Server]
    public IEnumerator ServerTimerCoroutine(int seconds)
    {
        if (!isServer)
            yield break;

        CurrentSeconds = seconds;

        while (CurrentSeconds > 0)
        {
            yield return _waitForOneSecond;
            ServerDecreaseTimeBy(1);
        }

        IsRunning = false;

        OnTimeRanOut.Invoke();
    }

    [Server]
    public void ServerDecreaseTimeBy(int seconds)
    {
        if (!isServer)
            return;

        CurrentSeconds = Mathf.Clamp
        (
            value: CurrentSeconds - seconds,
            min: 0,
            max: CurrentSeconds
        );
    }

    [Client]
    public void OnClientCurrentSecondsChanged(int oldValue, int newValue)
    {
        if (!isClient)
            return;

        OnUpdated.Invoke(newValue);
    }
}
