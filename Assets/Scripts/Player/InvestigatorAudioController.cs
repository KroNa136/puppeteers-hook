using System.Collections.Generic;
using UnityEngine;

public class InvestigatorAudioController : GamePlayerAudioController
{
    [SerializeField] private AudioSource _dyspneaAudioSource;
    [SerializeField] private AudioSource _flashlightAudioSource;
    [SerializeField] private AudioSource _interactionAudioSource;
    [SerializeField] private AudioSource _fearAudioSource;
    [SerializeField] private AudioSource _sanityDecreaseAudioSource;
    [SerializeField] private List<AudioSource> _lowSanityLossAudioSources;
    [SerializeField] private List<AudioSource> _highSanityLossAudioSources;
    [SerializeField] private AudioSource _amuletAudioSource;

    [Space]

    [SerializeField] private AudioClip _flashlightAudioClip;
    [SerializeField] private AudioClip _interactionAudioClip;
    [SerializeField] private AudioClip _interactionFailedAudioClip;
    [SerializeField] private AudioClip _startBeingAfraidAudioClip;
    [SerializeField] private List<AudioClip> _lowSanityLossAudioClips;
    [SerializeField] private List<AudioClip> _highSanityLossAudioClips;
    [SerializeField] private AudioClip _amuletBreakAudioClip;

    [Space]

    [SerializeField][Range(0.0f, 1.0f)] protected float _maxDyspneaVolume = 1f;
    [SerializeField] protected float _dyspneaVolumeChangeSpeed = 1f;

    [Space]

    [SerializeField][Range(0.0f, 1.0f)] protected float _maxFearVolume = 1f;
    [SerializeField] protected float _fearVolumeChangeSpeed = 1f;

    [Space]

    [SerializeField][Range(0.0f, 1.0f)] protected float _maxSanityDecreaseVolume = 1f;
    [SerializeField] protected float _sanityDecreaseVolumeChangeSpeed = 1f;

    [Space]

    public bool LowSanityLoss = false;
    [SerializeField] private float _lowSanityLossSoundsDelay = 5f;
    [SerializeField] private float _lowSanityLossSoundsPlayChance = 0.1f;

    private float _lowSanityLossSoundsTimer = 0f;

    [Space]

    public bool HighSanityLoss = false;
    [SerializeField] private float _highSanityLossSoundsDelay = 5f;
    [SerializeField] private float _highSanityLossSoundsPlayChance = 0.1f;

    private float _highSanityLossSoundsTimer = 0f;

    private void Update()
    {
        if (LowSanityLoss)
        {
            _lowSanityLossSoundsTimer += Time.deltaTime;

            if (_lowSanityLossSoundsTimer >= _lowSanityLossSoundsDelay)
            {
                _lowSanityLossSoundsTimer = 0f;

                if (_lowSanityLossAudioSources.None() || _lowSanityLossAudioClips.None())
                    return;

                bool play = Random.value <= _lowSanityLossSoundsPlayChance;

                if (play)
                    _ = _lowSanityLossAudioSources.UnityRandomItem().Bind(a => a.PlayOneShot(_lowSanityLossAudioClips.UnityRandomItem()));
            }
        }
        else
        {
            _lowSanityLossSoundsTimer = 0f;
        }

        if (HighSanityLoss)
        {
            _highSanityLossSoundsTimer += Time.deltaTime;

            if (_highSanityLossSoundsTimer >= _highSanityLossSoundsDelay)
            {
                _highSanityLossSoundsTimer = 0f;

                if (_highSanityLossAudioSources.None() || _highSanityLossAudioClips.None())
                    return;

                bool play = Random.value <= _highSanityLossSoundsPlayChance;

                if (play)
                    _ = _highSanityLossAudioSources.UnityRandomItem().Bind(a => a.PlayOneShot(_highSanityLossAudioClips.UnityRandomItem()));
            }
        }
        else
        {
            _highSanityLossSoundsTimer = 0f;
        }
    }

    public void SetDyspneaVolume(float volume)
    {
        _ = _dyspneaAudioSource.Bind
        (
            (audioSource, newVolume, volumeChangeSpeed) => audioSource.volume = Mathf.Lerp(audioSource.volume, newVolume, volumeChangeSpeed * Time.deltaTime),
            Mathf.Clamp01(volume) * _maxDyspneaVolume,
            _dyspneaVolumeChangeSpeed
        );
    }

    public void PlayFlashlightSound()
    {
        _ = _flashlightAudioSource.Bind(a => _flashlightAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayInteractionSound(bool successful)
    {
        _ = successful
            ? _interactionAudioSource.Bind(a => _interactionAudioClip.Bind(c => a.PlayOneShot(c)))
            : _interactionAudioSource.Bind(a => _interactionFailedAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayFearSound()
    {
        _ = _fearAudioSource.Bind(a => _startBeingAfraidAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void SetFearVolume(float volume)
    {
        _ = _fearAudioSource.Bind
        (
            (audioSource, newVolume, volumeChangeSpeed) => audioSource.volume = Mathf.Lerp(audioSource.volume, newVolume, volumeChangeSpeed * Time.deltaTime),
            Mathf.Clamp01(volume) * _maxFearVolume,
            _fearVolumeChangeSpeed
        );
    }

    public void StopFearSound()
    {
        _ = _fearAudioSource.Bind(a => a.Stop());
    }

    public void SetSanityDecreaseVolume(float volume)
    {
        _ = _sanityDecreaseAudioSource.Bind
        (
            (audioSource, newVolume, volumeChangeSpeed) => audioSource.volume = Mathf.Lerp(audioSource.volume, newVolume, volumeChangeSpeed * Time.deltaTime),
            Mathf.Clamp01(volume) * _maxSanityDecreaseVolume,
            _sanityDecreaseVolumeChangeSpeed
        );
    }

    public void PlayAmuletBreakSound()
    {
        _ = _amuletAudioSource.Bind(a => _amuletBreakAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
