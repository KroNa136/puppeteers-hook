using UnityEngine;

public class AmuletAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _pickUpAudioClip;

    public bool IsPlaying => _audioSource.isPlaying;

    public void PlayPickUpSound()
    {
        _ = _audioSource.Bind(a => _pickUpAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
