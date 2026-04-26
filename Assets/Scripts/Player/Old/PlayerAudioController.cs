using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("General References")]

    [SerializeField] CharacterController characterController;
    [SerializeField] PhysicMaterialManager physicMaterialManager;

    [Header("Audio Sources")]

    [SerializeField] AudioSource breathingAudioSource;
    [SerializeField] AudioSource[] movementAudioSources;
    [SerializeField] AudioSource windAudioSource;
    [SerializeField] AudioSource dyspneaAudioSource;
    [SerializeField] AudioSource flashlightAudioSource;
    [SerializeField] AudioSource interactionAudioSource;
    [SerializeField] AudioSource bedAudioSource;
    [SerializeField] AudioSource sanityDecreaseAreaAudioSource;
    [SerializeField] AudioSource lowSanityLossAudioSource;
    [SerializeField] AudioSource[] mediumSanityLossAudioSources;
    
    [Header("Audio Clips")]

    [SerializeField] AudioClip[] movementSounds;
    [SerializeField] AudioClip dyspneaSound;
    [SerializeField] AudioClip flashlightSound;
    [SerializeField] AudioClip interactionSound;
    [SerializeField] AudioClip interactionFailureSound;
    [SerializeField] AudioClip[] lowSanityLossSounds;
    [SerializeField] AudioClip[] mediumSanityLossSounds;

    [Header("Maximum Volumes")]

    [SerializeField] [Range(0.0f, 1.0f)] float movementAudioSourcesMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float windAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float dyspneaAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float flashlightAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float interactionAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float bedAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float sanityDecreaseAreaAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float lowSanityLossAudioSourceMaxVolume = 1f;
    [SerializeField] [Range(0.0f, 1.0f)] float mediumSanityLossAudioSourcesMaxVolume = 1f;

    [Header("Options")]

    [SerializeField] bool enableMovementSounds = true;
    [SerializeField] bool enableWindSounds = true;
    [SerializeField] bool enableDyspneaSounds = true;
    [SerializeField] bool enableFlashlightSounds = true;
    [SerializeField] bool enableInteractionSounds = true;
    [SerializeField] bool enableBedSounds = true;
    [SerializeField] bool enableSanityDecreaseAreaSounds = true;
    [SerializeField] bool enableLowSanityLossSounds = true;
    [SerializeField] bool enableMediumSanityLossSounds = true;

    [Header("Movement Sounds")]

    [SerializeField] float crouchingMovementVolumeMultiplier = 0.45f;
    [SerializeField] float movementVolumeChangeSpeed = 4f;

    [Header("Dyspnea Sounds")]

    [SerializeField] float dyspneaVolumeChangeSpeed = 1f;

    [Header("Wind Sounds")]

    [SerializeField] float windVolumeChangeSpeed = 4f;

    [Header("Low Sanity Loss Sounds")]

    [SerializeField] bool generateLowSanityLossSounds = false;
    [SerializeField] float timeIntervalForLowSanityLossSounds = 15f;
    [SerializeField] int chanceForLowSanityLossSounds = 4;
    float tForLowSanityLossSounds = 0f;
    [SerializeField] float minXShift = -10f;
    [SerializeField] float maxXShift = 10f;
    [SerializeField] float minYShiftMultiplier = -1f;
    [SerializeField] float maxYShiftMultiplier = 4f;
    [SerializeField] float minZShift = -10f;
    [SerializeField] float maxZShift = 10f;

    [Header("Medium Sanity Loss Sounds")]

    [SerializeField] bool generateMediumSanityLossSounds = false;
    [SerializeField] float timeIntervalForMediumSanityLossSounds = 30f;
    [SerializeField] int chanceForMediumSanityLossSounds = 4;
    float tForMediumSanityLossSounds = 0f;

    int rand;
    int randClipIndex;
    int randSourceIndex;

    int freeMovementAudioSource;
    float maxTime;
    int maxIndex;

    AudioClip[] sounds;

    void Start()
    {
        foreach (AudioSource audioSource in movementAudioSources)
            audioSource.volume = movementAudioSourcesMaxVolume;

        windAudioSource.volume = 0f;
        
        dyspneaAudioSource.volume = 0f;

        flashlightAudioSource.volume = flashlightAudioSourceMaxVolume;

        interactionAudioSource.volume = interactionAudioSourceMaxVolume;

        bedAudioSource.volume = bedAudioSourceMaxVolume;

        sanityDecreaseAreaAudioSource.volume = 0f;

        lowSanityLossAudioSource.volume = lowSanityLossAudioSourceMaxVolume;

        foreach (AudioSource audioSource in mediumSanityLossAudioSources)
            audioSource.volume = mediumSanityLossAudioSourcesMaxVolume;
    }

    void Update()
    {
        if (enableLowSanityLossSounds && generateLowSanityLossSounds)
        {
            tForLowSanityLossSounds += Time.deltaTime;

            if (tForLowSanityLossSounds >= timeIntervalForLowSanityLossSounds)
            {
                tForLowSanityLossSounds = 0f;

                rand = Random.Range(0, chanceForLowSanityLossSounds);

                if (rand == 0)
                {
                    float x = Random.Range(minXShift, maxXShift);
                    float y = Random.Range(characterController.height * minYShiftMultiplier, characterController.height * maxYShiftMultiplier);
                    float z = Random.Range(minZShift, maxZShift);

                    lowSanityLossAudioSource.transform.localPosition = new Vector3(x, y, z);

                    randClipIndex = Random.Range(0, lowSanityLossSounds.Length);

                    lowSanityLossAudioSource.Stop();
                    lowSanityLossAudioSource.PlayOneShot(lowSanityLossSounds[randClipIndex]);
                }
            }
        }

        if (enableMediumSanityLossSounds && generateMediumSanityLossSounds)
        {
            tForMediumSanityLossSounds += Time.deltaTime;

            if (tForMediumSanityLossSounds >= timeIntervalForMediumSanityLossSounds)
            {
                tForMediumSanityLossSounds = 0f;

                rand = Random.Range(0, chanceForMediumSanityLossSounds);

                if (rand == 0)
                {
                    randClipIndex = Random.Range(0, mediumSanityLossSounds.Length);
                    randSourceIndex = Random.Range(0, mediumSanityLossAudioSources.Length);

                    mediumSanityLossAudioSources[randSourceIndex].Stop();
                    mediumSanityLossAudioSources[randSourceIndex].PlayOneShot(mediumSanityLossSounds[randClipIndex]);
                }
            }
        }
    }

    public void SetMovementSounds(PhysicsMaterial material)
    {
        if (material == null)
            return;

        movementSounds = physicMaterialManager.GetSounds(material);
    }

    public void PlayMovementSound(int index)
    {
        if (!enableMovementSounds || movementSounds.Length == 0)
            return;

        freeMovementAudioSource = FreeAudioSource(movementAudioSources);

        if (freeMovementAudioSource == -1)
        {
            maxTime = 0f;
            maxIndex = 0;

            for (int i = 0; i < movementAudioSources.Length; i++)
            {
                if (movementAudioSources[i].time > maxTime)
                {
                    maxTime = movementAudioSources[i].time;
                    maxIndex = i;
                }
            }

            movementAudioSources[maxIndex].Stop();
            movementAudioSources[maxIndex].PlayOneShot(movementSounds[index]);
        }
        else
        {
            movementAudioSources[freeMovementAudioSource].PlayOneShot(movementSounds[index]);
        }
    }

    public void PlayMovementSound(AudioClip audioClip)
    {
        if (!enableMovementSounds)
            return;

        freeMovementAudioSource = FreeAudioSource(movementAudioSources);

        if (freeMovementAudioSource == -1)
        {
            maxTime = 0f;
            maxIndex = 0;

            for (int i = 0; i < movementAudioSources.Length; i++)
            {
                if (movementAudioSources[i].time > maxTime)
                {
                    maxTime = movementAudioSources[i].time;
                    maxIndex = i;
                }
            }

            movementAudioSources[maxIndex].Stop();
            movementAudioSources[maxIndex].PlayOneShot(audioClip);
        }
        else
        {
            movementAudioSources[freeMovementAudioSource].PlayOneShot(audioClip);
        }
    }

    public void PlayFootstepSound()
    {
        if (enableMovementSounds && movementSounds.Length > 0)
            PlayMovementSound(Random.Range(0,3));
    }

    public void PlayJumpSound()
    {
        if (enableMovementSounds && movementSounds.Length > 0)
            PlayMovementSound(3);
    }

    public void PlayLandingSound()
    {
        if (enableMovementSounds && movementSounds.Length > 0)
            PlayMovementSound(4);
    }

    public void PlayHitSound(PhysicsMaterial material)
    {
        if (enableMovementSounds)
        {
            sounds = physicMaterialManager.GetSounds(material);
            
            if (sounds.Length > 0)
                PlayMovementSound(sounds[5]);
        }
    }

    public void SetCrouchingMovementVolume()
    {
        if (enableMovementSounds)
        {
            for (int i = 0; i < movementAudioSources.Length; i++)
                movementAudioSources[i].volume = Mathf.Lerp(movementAudioSources[i].volume, movementAudioSourcesMaxVolume * crouchingMovementVolumeMultiplier, movementVolumeChangeSpeed * Time.deltaTime);
        }
    }

    public void SetNormalMovementVolume()
    {
        if (enableMovementSounds)
        {
            for (int i = 0; i < movementAudioSources.Length; i++)
                movementAudioSources[i].volume = Mathf.Lerp(movementAudioSources[i].volume, movementAudioSourcesMaxVolume, movementVolumeChangeSpeed * Time.deltaTime);
        }
    }

    public void SetWindSoundsVolume(float volume)
    {
        if (enableWindSounds)
            windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, Mathf.Clamp01(volume) * windAudioSourceMaxVolume, windVolumeChangeSpeed * Time.deltaTime);
    }

    public void SetDyspneaSoundVolume(float volume)
    {
        if (enableDyspneaSounds)
            dyspneaAudioSource.volume = Mathf.Lerp(dyspneaAudioSource.volume, Mathf.Clamp01(volume) * dyspneaAudioSourceMaxVolume, dyspneaVolumeChangeSpeed * Time.deltaTime);
    }

    public void PlayFlashlightSound()
    {
        if (enableFlashlightSounds)
        {
            flashlightAudioSource.Stop();
            flashlightAudioSource.PlayOneShot(flashlightSound);
        }
    }

    public void PlayInteractionSound(bool interactionSuccess)
    {
        if (enableInteractionSounds)
        {
            interactionAudioSource.Stop();

            if (interactionSuccess)
                interactionAudioSource.PlayOneShot(interactionSound);
            else
                interactionAudioSource.PlayOneShot(interactionFailureSound);
        }
    }

    public void SetSanityDecreaseAreaVolume(float volume)
    {
        if (enableSanityDecreaseAreaSounds)
            sanityDecreaseAreaAudioSource.volume = Mathf.Clamp01(volume) * sanityDecreaseAreaAudioSourceMaxVolume;
    }

    public void SetLowSanityLossSounds(bool value)
    {
        if (enableLowSanityLossSounds)
            generateLowSanityLossSounds = value;
    }

    public void SetMediumSanityLossSounds(bool value)
    {
        if (enableMediumSanityLossSounds)
            generateMediumSanityLossSounds = value;
    }

    int FreeAudioSource(AudioSource[] audioSources)
    {
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (!audioSources[i].isPlaying)
                return i;
        }

        return -1;
    }

    [ContextMenu("Disable All Sounds")]
    void DisableAllSounds()
    {
        enableMovementSounds = false;
        enableWindSounds = false;
        enableDyspneaSounds = false;
        enableFlashlightSounds = false;
        enableInteractionSounds = false;
        enableBedSounds = false;
        enableSanityDecreaseAreaSounds = false;
        enableLowSanityLossSounds = false;
        enableMediumSanityLossSounds = false;
    }

    [ContextMenu("Enable All Sounds")]
    void EnableAllSounds()
    {
        enableMovementSounds = true;
        enableWindSounds = true;
        enableDyspneaSounds = true;
        enableFlashlightSounds = true;
        enableInteractionSounds = true;
        enableBedSounds = true;
        enableSanityDecreaseAreaSounds = true;
        enableLowSanityLossSounds = true;
        enableMediumSanityLossSounds = true;
    }
}
