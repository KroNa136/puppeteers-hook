using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicController : MonoBehaviour
{
    public static MusicController Instance;

    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private List<AudioClip> _menuAudioClips;
    [SerializeField] private List<AudioClip> _ghostPreparePhaseAudioClips;
    [SerializeField] private List<AudioClip> _mainPhaseAudioClips;

    [Space]

    [SerializeField] private float _fadeDuration = 0.5f;

    [Space]

    [SerializeField][Scene] private string _menuScene;

    private float _notPlayingOnMenuSceneTimer = 0f;

    public int RandomMenuMusicIndex => _menuAudioClips.None() ? -1 : Random.Range(0, _menuAudioClips.Count);
    public int RandomGhostPreparePhaseMusicIndex => _ghostPreparePhaseAudioClips.None() ? -1 : Random.Range(0, _ghostPreparePhaseAudioClips.Count);
    public int RandomMainPhaseMusicIndex => _mainPhaseAudioClips.None() ? -1 : Random.Range(0, _mainPhaseAudioClips.Count);

    public bool IsPlaying => _audioSource.TryGet(a => a.isPlaying, out bool playing) && playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMenuMusic(RandomMenuMusicIndex);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().path.Equals(_menuScene) && !IsPlaying)
        {
            _notPlayingOnMenuSceneTimer += Time.deltaTime;

            if (_notPlayingOnMenuSceneTimer > 0.25f)
                PlayMenuMusic(RandomMenuMusicIndex);
        }
        else
        {
            _notPlayingOnMenuSceneTimer = 0f;
        }
    }

    public void PlayMenuMusic(int index)
    {
        if (_menuAudioClips.None())
            return;
        
        if (index < 0 || index >= _menuAudioClips.Count)
            return;

        _ = StartCoroutine(FadeToAudioClip(_menuAudioClips[index]));
    }

    public void PlayGhostPreparePhaseMusic(int index)
    {
        if (_ghostPreparePhaseAudioClips.None())
            return;

        if (index < 0 || index >= _menuAudioClips.Count)
            return;

        _ = StartCoroutine(FadeToAudioClip(_ghostPreparePhaseAudioClips[index]));
    }

    public void PlayMainPhaseMusic(int index)
    {
        if (_mainPhaseAudioClips.None())
            return;

        if (index < 0 || index >= _menuAudioClips.Count)
            return;

        _ = StartCoroutine(FadeToAudioClip(_mainPhaseAudioClips[index]));
    }

    public void StopMusic()
    {
        _ = StartCoroutine(FadeToAudioClip(null));
    }

    private IEnumerator FadeToAudioClip(AudioClip audioClip)
    {
        if (_audioSource == null)
            yield break;

        if (IsPlaying)
        {
            float fadeOutTimer = 0f;

            while (fadeOutTimer < _fadeDuration)
            {
                fadeOutTimer += Time.deltaTime;
                _audioSource.volume = 1f - (fadeOutTimer / _fadeDuration);

                yield return null;
            }
        }

        _audioSource.Stop();
        _audioSource.volume = 0f;

        if (audioClip == null)
            yield break;

        _audioSource.clip = audioClip;
        _audioSource.Play();

        float fadeInTimer = 0f;

        while (fadeInTimer < _fadeDuration)
        {
            fadeInTimer += Time.deltaTime;
            _audioSource.volume = fadeInTimer / _fadeDuration;

            yield return null;
        }

        _audioSource.volume = 1f;
    }
}
