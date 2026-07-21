using UnityEngine;

public class StatueAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _rotationAudioClip;
    [SerializeField] private AudioClip _stopRotationAudioClip;

    private bool _isPlayingRotationSound = false;
    public bool IsPlayingRotationSound => _isPlayingRotationSound;

    public void PlayRotationSound()
    {
        _ = _audioSource
            .Bind(a => _rotationAudioClip.Bind(c => a.PlayOneShot(c)))
            .Bind(a => _isPlayingRotationSound = true);
    }

    public void PlayStopRotationSound()
    {
        _ = _audioSource
            .Bind(a => a.Stop())
            .Bind(a => _stopRotationAudioClip.Bind(c => a.PlayOneShot(c)));

        _isPlayingRotationSound = false;
    }
}
