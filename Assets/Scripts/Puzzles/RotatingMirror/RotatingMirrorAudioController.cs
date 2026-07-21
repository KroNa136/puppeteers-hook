using UnityEngine;

public class RotatingMirrorAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _rotationAudioClip;

    public bool IsPlaying => _audioSource.isPlaying;

    public void PlayRotationSound()
    {
        _ = _audioSource.Bind(a => _rotationAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void StopRotationSound()
    {
        _ = _audioSource.Bind(a => a.Stop());
    }
}
