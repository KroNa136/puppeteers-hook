using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAudioController : MonoBehaviour
{
    [Header("Audio Sources")]

    [SerializeField] AudioSource openingClosingAudioSource;
    [SerializeField] AudioSource highSanityLossAudioSource;

    [Header("Audio Clips")]

    [SerializeField] AudioClip openingSound;
    [SerializeField] AudioClip closingSound;
    [SerializeField] AudioClip lockedSound;
    [SerializeField] AudioClip unlockedSound;
    [SerializeField] AudioClip[] highSanityLossSounds;

    [Header("Maximum Volumes")]

    [SerializeField] [Range(0.0f, 1.0f)] float openingClosingAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float highSanityLossSoundsMaxVolume = 1f;

    [Header("Options")]

    [SerializeField] bool enableOpeningClosingSounds = true;
    public bool enableHighSanityLossSounds = true;

    [Header("High Sanity Loss Sounds")]

    [SerializeField] bool generateHighSanityLossSounds = false;
    [SerializeField] float timeIntervalForHighSanityLossSounds = 15f;
    [SerializeField] int chanceForHighSanityLossSounds = 10;
    float tForHighSanityLossSounds = 0f;

    int rand;
    int randClipIndex;

    void Start()
    {
        if (openingClosingAudioSource != null)
            openingClosingAudioSource.volume = openingClosingAudioSourceMaxVolume;

        if (highSanityLossAudioSource != null)
            highSanityLossAudioSource.volume = highSanityLossSoundsMaxVolume;
    }

    void Update()
    {
        if (enableHighSanityLossSounds && generateHighSanityLossSounds)
        {
            tForHighSanityLossSounds += Time.deltaTime;

            if (tForHighSanityLossSounds >= timeIntervalForHighSanityLossSounds)
            {
                tForHighSanityLossSounds = 0f;

                rand = Random.Range(0, chanceForHighSanityLossSounds);

                if (rand == 0)
                {
                    randClipIndex = Random.Range(0, highSanityLossSounds.Length);

                    highSanityLossAudioSource.Stop();
                    highSanityLossAudioSource.PlayOneShot(highSanityLossSounds[randClipIndex]);
                }
            }
        }
    }

    public void PlayOpeningSound()
    {
        if (enableOpeningClosingSounds)
        {
            openingClosingAudioSource.Stop();
            openingClosingAudioSource.PlayOneShot(openingSound);
        }
    }

    public void PlayClosingSound()
    {
        if (enableOpeningClosingSounds)
        {
            openingClosingAudioSource.Stop();
            openingClosingAudioSource.PlayOneShot(closingSound);
        }
    }

    public void PlayLockedSound()
    {
        if (enableOpeningClosingSounds)
        {
            openingClosingAudioSource.Stop();
            openingClosingAudioSource.PlayOneShot(lockedSound);
        }
    }

    public void PlayUnlockedSound()
    {
        if (enableOpeningClosingSounds)
        {
            openingClosingAudioSource.Stop();
            openingClosingAudioSource.PlayOneShot(unlockedSound);
        }
    }

    public void SetHighSanityLossSounds(bool value)
    {
        if (enableHighSanityLossSounds)
            generateHighSanityLossSounds = value;
    }

    [ContextMenu("Disable All Sounds")]
    void DisableAllSounds()
    {
        enableOpeningClosingSounds = false;
        enableHighSanityLossSounds = false;
    }

    [ContextMenu("Enable All Sounds")]
    void EnableAllSounds()
    {
        enableOpeningClosingSounds = true;
        enableHighSanityLossSounds = true;
    }
}
