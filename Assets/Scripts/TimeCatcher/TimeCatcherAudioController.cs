using System.Collections.Generic;
using UnityEngine;

public class TimeCatcherAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _movementAudioSource;
    [SerializeField] private AudioSource _attackAudioSource;

    [Space]

    [SerializeField] private List<AudioClip> _footstepAudioClips;
    [SerializeField] private AudioClip _attackAudioClip;

    public bool IsPlaying => _movementAudioSource.isPlaying || _attackAudioSource.isPlaying;

    public void PlayFootstepSound()
    {
        if (_footstepAudioClips.None())
            return;

        _ = _movementAudioSource.Bind(a => a.PlayOneShot(_footstepAudioClips.UnityRandomItem()));
    }

    public void PlayAttackSound()
    {
        _ = _attackAudioSource.Bind(a => _attackAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
