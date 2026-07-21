using System.Collections.Generic;
using UnityEngine;

public abstract class GamePlayerAudioController : MonoBehaviour
{
    [SerializeField] protected AudioSource _movementAudioSource;
    [SerializeField] protected AudioSource _windAudioSource;

    [Space]

    [SerializeField] protected List<AudioClip> _footstepAudioClips;
    [SerializeField] protected AudioClip _landingAudioClip;

    [Space]

    [SerializeField][Range(0.0f, 1.0f)] protected float _maxWindVolume = 1f;
    [SerializeField] protected float _windVolumeChangeSpeed = 3f;

    public void PlayFootstepSound()
    {
        if (_footstepAudioClips.None())
            return;

        _ = _movementAudioSource.Bind(a => a.PlayOneShot(_footstepAudioClips.UnityRandomItem()));
    }

    public void PlayLandingSound()
    {
        _ = _movementAudioSource.Bind(a => _landingAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void SetWindVolume(float volume)
    {
        _ = _windAudioSource.Bind
        (
            (audioSource, newVolume, volumeChangeSpeed) => audioSource.volume = Mathf.Lerp(audioSource.volume, newVolume, volumeChangeSpeed * Time.deltaTime),
            Mathf.Clamp01(volume) * _maxWindVolume,
            _windVolumeChangeSpeed
        );
    }
}
