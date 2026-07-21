using UnityEngine;

public class CandlestickAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _lightAudioClip;
    [SerializeField] private AudioClip _putOutAudioClip;

    public void PlayLightSound()
    {
        _ = _audioSource.Bind(a => _lightAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayPutOutSound()
    {
        _ = _audioSource.Bind(a => _putOutAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
