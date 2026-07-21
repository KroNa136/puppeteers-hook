using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource _openingClosingAudioSource;
    [SerializeField] private AudioSource _lockUnlockAudioSource;
    [SerializeField] private AudioSource _sanityLossAudioSource;

    [Space]

    [SerializeField] private AudioClip _openingAudioClip;
    [SerializeField] private AudioClip _closingAudioClip;
    [SerializeField] private AudioClip _lockAudioClip;
    [SerializeField] private AudioClip _unlockAudioClip;
    [SerializeField] private List<AudioClip> _sanityLossAudioClips;

    [Space]

    public bool CanPlaySanityLossSounds = true;
    public bool PlayerHasSanityLoss = false;
    [SerializeField] private float _sanityLossSoundsDelay = 5f;
    [SerializeField] private float _sanityLossSoundsPlayChance = 0.1f;

    private float _sanityLossSoundsTimer = 0f;
    
    private void Update()
    {
        if (CanPlaySanityLossSounds && PlayerHasSanityLoss)
        {
            _sanityLossSoundsTimer += Time.deltaTime;

            if (_sanityLossSoundsTimer >= _sanityLossSoundsDelay)
            {
                _sanityLossSoundsTimer = 0f;

                if (_sanityLossAudioClips.None())
                    return;

                bool play = Random.value <= _sanityLossSoundsPlayChance;

                if (play)
                    _ = _sanityLossAudioSource.Bind(a => a.PlayOneShot(_sanityLossAudioClips.UnityRandomItem()));
            }
        }
        else
        {
            _sanityLossSoundsTimer = 0f;
        }
    }

    public void PlayOpeningSound()
    {
        _ = _openingClosingAudioSource
            .Bind(a => a.Stop())
            .Bind(a => _openingAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayClosingSound()
    {
        _ = _openingClosingAudioSource
            .Bind(a => a.Stop())
            .Bind(a => _closingAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayLockSound()
    {
        _ = _lockUnlockAudioSource
            .Bind(a => a.Stop())
            .Bind(a => _lockAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayUnlockSound()
    {
        _ = _lockUnlockAudioSource
            .Bind(a => a.Stop())
            .Bind(a => _unlockAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}
