using UnityEngine;

public class NoteAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _startReadingAudioClip;
    [SerializeField] private AudioClip _stopReadingAudioClip;

    public void PlayStartReadingSound()
    {
        _ = _audioSource.Bind(a => _startReadingAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayStopReadingSound()
    {
        _ = _audioSource.Bind(a => _stopReadingAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
