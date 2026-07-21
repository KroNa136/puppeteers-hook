using UnityEngine;

public class DrawerAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _openingAudioClip;
    [SerializeField] private AudioClip _closingAudioClip;

    public void PlayOpeningSound()
    {
        _ = _audioSource.Bind(a => _openingAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayClosingSound()
    {
        _ = _audioSource.Bind(a => _closingAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
