using UnityEngine;

public class HoldableAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _hitAudioClip;

    public void PlayHitSound()
    {
        _ = _audioSource.Bind(a => _hitAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
