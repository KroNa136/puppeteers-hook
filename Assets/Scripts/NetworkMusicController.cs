using System.Collections;
using Mirror;
using UnityEngine;

public class NetworkMusicController : NetworkBehaviour
{
    private MusicController _musicController;

    private Coroutine _playGhostPreparePhaseMusicRoutine;
    private Coroutine _playMainPhaseMusicRoutine;

    private float _notPlayingTimer = 0f;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _musicController = MusicController.Instance;

        GameManager.OnServerGhostPreparePhaseStarted.AddListener(ServerGhostPreparePhaseStarted);
        GameManager.OnServerMainPhaseStarted.AddListener(ServerMainPhaseStarted);
        GameManager.OnServerGameOver.AddListener(ServerGameOver);
    }

    [Server]
    public void ServerGhostPreparePhaseStarted()
    {
        if (!isServer)
            return;

        _playGhostPreparePhaseMusicRoutine = StartCoroutine(ServerPlayGhostPreparePhaseMusic());
    }

    [Server]
    public IEnumerator ServerPlayGhostPreparePhaseMusic()
    {
        if (!isServer)
            yield break;

        ClientStopMusic();
        RpcStopMusic();

        while (true)
        {
            if (!_musicController.IsPlaying)
            {
                _notPlayingTimer += Time.deltaTime;

                if (_notPlayingTimer > 0.25f)
                {
                    int index = _musicController.RandomGhostPreparePhaseMusicIndex;
                    ClientPlayGhostPreparePhaseMusic(index);
                    RpcPlayGhostPreparePhaseMusic(index);

                    _notPlayingTimer = 0f;
                }
            }
            else
            {
                _notPlayingTimer = 0f;
            }

            yield return null;
        }
    }

    [Server]
    public void ServerMainPhaseStarted()
    {
        if (!isServer)
            return;

        if (_playGhostPreparePhaseMusicRoutine != null)
        {
            StopCoroutine(_playGhostPreparePhaseMusicRoutine);
            _playGhostPreparePhaseMusicRoutine = null;
        }

        _playMainPhaseMusicRoutine = StartCoroutine(ServerPlayMainPhaseMusic());
    }

    [Server]
    public IEnumerator ServerPlayMainPhaseMusic()
    {
        if (!isServer)
            yield break;

        ClientStopMusic();
        RpcStopMusic();

        while (true)
        {
            if (!_musicController.IsPlaying)
            {
                _notPlayingTimer += Time.deltaTime;

                if (_notPlayingTimer > 0.25f)
                {
                    int index = _musicController.RandomMainPhaseMusicIndex;
                    ClientPlayMainPhaseMusic(index);
                    RpcPlayMainPhaseMusic(index);

                    _notPlayingTimer = 0f;
                }
            }
            else
            {
                _notPlayingTimer = 0f;
            }

            yield return null;
        }
    }

    [Server]
    public void ServerGameOver()
    {
        if (_playGhostPreparePhaseMusicRoutine != null)
        {
            StopCoroutine(_playGhostPreparePhaseMusicRoutine);
            _playGhostPreparePhaseMusicRoutine = null;
        }

        if (_playMainPhaseMusicRoutine != null)
        {
            StopCoroutine(_playMainPhaseMusicRoutine);
            _playMainPhaseMusicRoutine = null;
        }

        ClientStopMusic();
        RpcStopMusic();
    }

    [ClientRpc]
    public void RpcPlayGhostPreparePhaseMusic(int index)
    {
        if (isServer)
            return;

        ClientPlayGhostPreparePhaseMusic(index);
    }

    [Client]
    public void ClientPlayGhostPreparePhaseMusic(int index)
    {
        if (!isClient)
            return;

        MusicController.Instance.PlayGhostPreparePhaseMusic(index);
    }

    [ClientRpc]
    public void RpcPlayMainPhaseMusic(int index)
    {
        if (isServer)
            return;

        ClientPlayMainPhaseMusic(index);
    }

    [Client]
    public void ClientPlayMainPhaseMusic(int index)
    {
        if (!isClient)
            return;

        MusicController.Instance.PlayMainPhaseMusic(index);
    }

    [ClientRpc]
    public void RpcStopMusic()
    {
        if (isServer)
            return;

        ClientStopMusic();
    }

    [Client]
    public void ClientStopMusic()
    {
        if (!isClient)
            return;

        MusicController.Instance.StopMusic();
    }
}
